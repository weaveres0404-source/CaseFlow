import json
import re
import tempfile
from datetime import date, timedelta
from pathlib import Path

import requests
from playwright.sync_api import TimeoutError as PlaywrightTimeoutError
from playwright.sync_api import sync_playwright


BASE_UI = "https://caseflow-test.sld-lwd.com"
BASE_API = f"{BASE_UI}/api/v1"
PASSWORD = "@sld123456"
CHROME_PATH = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
RESULT_PATH = Path(__file__).with_name("manual_ui_blue_results.json")
PROJECT_SM_VALUE = "1"
CATEGORY_VALUES = {
    "日常操作異常": "2",
}
MODULE_VALUES = {
    "話務通話模組": "1",
}


class ResultLog:
    def __init__(self):
        self.items = []

    def add(self, case_id, status, note):
        safe_note = str(note).encode("cp950", errors="replace").decode("cp950", errors="replace")
        print(f"[{status}] {case_id} - {safe_note}")
        self.items.append({"id": case_id, "status": status, "note": note})

    def write(self):
        RESULT_PATH.write_text(json.dumps(self.items, ensure_ascii=False, indent=2), encoding="utf-8")


class Api:
    def __init__(self):
        self.session = requests.Session()
        self.tokens = {}
        self.users = {}
        self.meta = None
        self._bootstrap()

    def _bootstrap(self):
        for username in ["luffy", "zoro", "conan", "nami", "sasuke", "goku", "admin"]:
            self.tokens[username] = self.login(username)
        self.meta = self.request("GET", "/meta/dropdowns", "luffy").json()["data"]
        self.users = {user["username"]: user["id"] for user in self.meta["users"]}

    def login(self, username):
        response = self.session.post(
            f"{BASE_API}/auth/login",
            json={"username": username, "password": PASSWORD},
            timeout=30,
        )
        response.raise_for_status()
        return response.json()["data"]["access_token"]

    def request(self, method, path, username, **kwargs):
        headers = kwargs.pop("headers", {})
        headers["Authorization"] = f"Bearer {self.tokens[username]}"
        headers["Accept"] = "application/json"
        return self.session.request(method, f"{BASE_API}{path}", headers=headers, timeout=30, **kwargs)

    def project(self, code):
        return next(project for project in self.meta["projects"] if project.get("code") == code)

    def first_category(self, project_id, case_type_filter):
        return next(
            category
            for category in self.meta["categories"]
            if category.get("case_type_filter") == case_type_filter and project_id in category.get("project_ids", [])
        )

    def category_by_name(self, project_id, case_type_filter, name):
        return next(
            category
            for category in self.meta["categories"]
            if category.get("case_type_filter") == case_type_filter
            and project_id in category.get("project_ids", [])
            and category["name"] == name
        )

    def module_by_name(self, project_id, name):
        return next(module for module in self.meta["modules"] if module["project_id"] == project_id and module["name"] == name)

    def first_module(self, project_id):
        return next(module for module in self.meta["modules"] if module["project_id"] == project_id)

    def create_case(
        self,
        description,
        case_type="REPAIR",
        category_name=None,
        module_name=None,
        reporter_name="UI 測試員",
        username="luffy",
    ):
        project = self.project("SM-MAINT")
        if category_name:
            category = self.category_by_name(project["id"], case_type, category_name)
        else:
            category = self.first_category(project["id"], case_type)
        if module_name:
            module = self.module_by_name(project["id"], module_name)
        else:
            module = self.first_module(project["id"])
        payload = {
            "project_id": project["id"],
            "customer_id": project["customer_id"],
            "category_id": category["id"],
            "module_id": module["id"],
            "case_type": case_type,
            "priority": "MEDIUM",
            "reporter_name": reporter_name,
            "reporter_phone": "0900000000",
            "reporter_email": "ui-blue@test.local",
            "description": description,
        }
        response = self.request("POST", "/cases", username, json=payload)
        response.raise_for_status()
        return response.json()["data"]

    def assign(self, case_id, usernames, by="luffy"):
        se_ids = [self.users[username] for username in usernames]
        primary = se_ids[0]
        response = self.request(
            "POST",
            f"/cases/{case_id}/assign",
            by,
            json={"se_user_ids": se_ids, "primary_se_user_id": primary},
        )
        response.raise_for_status()
        return response

    def add_log(self, case_id, username, hours=1, handling_method="處理測試", result="結果測試"):
        response = self.request(
            "POST",
            f"/cases/{case_id}/logs",
            username,
            json={
                "handling_method": handling_method,
                "result": result,
                "hours_spent": hours,
                "log_date": str(date.today()),
            },
        )
        response.raise_for_status()
        return response

    def complete(self, case_id, username):
        response = self.request("POST", f"/cases/{case_id}/complete", username, json={})
        response.raise_for_status()
        return response

    def close(self, case_id, by="luffy"):
        response = self.request("POST", f"/cases/{case_id}/close", by, json={"close_remark": "UI close"})
        response.raise_for_status()
        return response

    def return_case(self, case_id, by="luffy"):
        response = self.request("POST", f"/cases/{case_id}/return", by, json={"reason": "UI return"})
        response.raise_for_status()
        return response

    def cancel(self, case_id, by="luffy"):
        response = self.request("POST", f"/cases/{case_id}/cancel", by, json={"reason": "UI cancel"})
        response.raise_for_status()
        return response


class Ui:
    def __init__(self):
        self.pw = sync_playwright().start()
        self.browser = self.pw.chromium.launch(headless=True, executable_path=CHROME_PATH)
        self.page = self.browser.new_page(viewport={"width": 1600, "height": 2000})

    def close(self):
        self.browser.close()
        self.pw.stop()

    def body_text(self):
        return self.page.locator("body").inner_text()

    def login(self, username, password=PASSWORD):
        self.page.goto(f"{BASE_UI}/login", wait_until="networkidle", timeout=60000)
        self.page.fill('input[name="account"]', username)
        self.page.fill('input[name="password"]', password)
        self.page.locator("button").last.click()
        self.page.wait_for_timeout(4000)
        try:
            self.page.wait_for_url(re.compile(r".*/(dashboard|setup-password|login.*)$"), timeout=30000)
        except PlaywrightTimeoutError:
            self.page.wait_for_timeout(3000)
        self.page.wait_for_load_state("networkidle")

    def logout(self):
        if "/login" not in self.page.url:
            self.click_button("登出")
            try:
                self.page.wait_for_url("**/login", timeout=15000)
            except PlaywrightTimeoutError:
                self.page.goto(f"{BASE_UI}/login", wait_until="networkidle", timeout=60000)
            self.page.wait_for_load_state("networkidle")

    def click_button(self, text):
        buttons = self.page.locator("button")
        labels = buttons.all_inner_texts()
        for index, label in enumerate(labels):
            if text == label.strip() or text in label.strip():
                buttons.nth(index).click()
                return
        raise AssertionError(f"button not found: {text}")

    def goto_case(self, slug):
        self.page.goto(f"{BASE_UI}/cases/{slug}", wait_until="networkidle", timeout=60000)

    def goto_new_case(self):
        self.page.goto(f"{BASE_UI}/cases/new", wait_until="networkidle", timeout=60000)

    def goto_dashboard(self):
        self.page.goto(f"{BASE_UI}/dashboard", wait_until="networkidle", timeout=60000)

    def goto_cases(self):
        self.page.goto(f"{BASE_UI}/cases", wait_until="networkidle", timeout=60000)

    def expect_contains(self, text):
        assert text in self.body_text(), f"missing text: {text}"

    def expect_not_contains(self, text):
        assert text not in self.body_text(), f"unexpected text: {text}"

    def fill_new_case_form(self, reporter_name, phone, email, case_type, category_label, module_label, description):
        selects = self.page.locator("select")
        selects.nth(0).select_option(value=PROJECT_SM_VALUE)
        self.page.wait_for_timeout(1500)
        self.page.locator('input[placeholder="客戶端聯絡人姓名"]').fill(reporter_name)
        self.page.locator('input[placeholder="02-xxxxxxxx / 09xx"]').fill(phone)
        self.page.locator('input[type="email"]').fill(email)
        self.click_button(case_type)
        selects.nth(1).select_option(value=CATEGORY_VALUES[category_label])
        self.page.wait_for_timeout(1000)
        self.page.wait_for_function(
            "() => document.querySelectorAll('select')[2] && document.querySelectorAll('select')[2].options.length > 1",
            timeout=10000,
        )
        selects.nth(2).select_option(value=MODULE_VALUES[module_label])
        self.page.locator('input[type="date"]').fill(str(date.today() + timedelta(days=1)))
        selects.nth(3).select_option(label="09:00")
        self.page.locator("textarea").fill(description)

    def newest_case_number_from_detail(self):
        body = self.body_text()
        match = re.search(r"SM-\d{6}-\d+", body)
        assert match, "case number not found"
        return match.group(0)

    def case_number_on_dashboard(self):
        body = self.body_text()
        return re.findall(r"SM-\d{6}-\d+", body)


def main():
    results = ResultLog()
    api = Api()
    ui = Ui()

    temp_dir = Path(tempfile.mkdtemp(prefix="caseflow-ui-"))
    large_file = temp_dir / "oversize.bin"
    invalid_file = temp_dir / "invalid.exe"
    large_file.write_bytes(b"0" * (21 * 1024 * 1024))
    invalid_file.write_bytes(b"MZ test")

    try:
        ui.login("luffy")
        results.add("TC-A01", "PASS" if "/dashboard" in ui.page.url and "蒙其·D·魯夫" in ui.body_text() and "新增案件" in ui.body_text() else "FAIL", ui.page.url)

        ui.logout()
        ui.login("nami")
        body = ui.body_text()
        results.add("TC-A02", "PASS" if "/dashboard" in ui.page.url and "娜美" in body and "新增案件" not in body else "FAIL", ui.page.url)

        ui.logout()
        ui.login("luffy", "wrongpwd")
        body = ui.body_text()
        results.add("TC-A03", "PASS" if "/login" in ui.page.url and ("登入" in body) else "FAIL", ui.page.url)

        results.add("TC-A04", "SKIP", "需 DBA 建立 must_change_password=TRUE 帳號")

        ui.login("nami")
        body = ui.body_text()
        menu_ok = all(label in body for label in ["儀表板", "案件列表", "通知中心", "個人設定"])
        results.add("TC-A05", "PASS" if menu_ok and "新增案件" not in body else "FAIL", "SE menu")

        unassigned = api.create_case("A06 未指派案件")
        ui.logout()
        ui.login("conan")
        ui.goto_case(unassigned["short_id"])
        body = ui.body_text()
        results.add("TC-A06", "PASS" if "找不到此案件" in body or "無權存取" in body else "FAIL", ui.page.url)

        ui.logout()
        ui.login("goku")
        ui.goto_case(unassigned["short_id"])
        body = ui.body_text()
        results.add("TC-A07", "PASS" if "找不到此案件" in body or "無權存取" in body else "FAIL", ui.page.url)

        assigned = api.create_case("A08 指派給佐助")
        api.assign(assigned["id"], ["sasuke"])
        ui.logout()
        ui.login("sasuke")
        ui.goto_case(assigned["short_id"])
        results.add("TC-A08", "PASS" if assigned["case_number"] in ui.body_text() else "FAIL", ui.page.url)

        completed = api.create_case("A09 完工案件")
        api.assign(completed["id"], ["sasuke"])
        api.add_log(completed["id"], "sasuke", hours=1)
        api.complete(completed["id"], "sasuke")
        ui.goto_case(completed["short_id"])
        results.add("TC-A09", "PASS" if "確認結案" not in ui.body_text() else "FAIL", "SE close button hidden")

        ui.logout()
        results.add("TC-A10", "PASS" if "/login" in ui.page.url else "FAIL", ui.page.url)

        ui.login("luffy")
        ui.goto_new_case()
        ui.fill_new_case_form(
            reporter_name="王經理",
            phone="0912345678",
            email="wang@example.com",
            case_type="障礙調查",
            category_label="日常操作異常",
            module_label="話務通話模組",
            description="TC-B01 手動建案測試",
        )
        ui.click_button("建立案件")
        try:
            ui.page.wait_for_url(re.compile(r".*/cases/[^/]+$"), timeout=30000)
        except PlaywrightTimeoutError:
            ui.page.wait_for_timeout(3000)
        ui.page.wait_for_load_state("networkidle")
        match = re.search(r"SM-\d{6}-\d+", ui.body_text())
        if match:
            b01_case_number = match.group(0)
            body = ui.body_text()
            results.add("TC-B01", "PASS" if "待處理" in body and b01_case_number.startswith("SM-") else "FAIL", b01_case_number)
        elif "/cases/" in ui.page.url and not ui.page.url.endswith("/cases/new"):
            slug = ui.page.url.rstrip("/").split("/")[-1]
            detail = api.request("GET", f"/cases/{slug}", "luffy").json()["data"]
            b01_case_number = detail["case_number"]
            body = ui.body_text()
            results.add("TC-B01", "PASS" if "待處理" in body and b01_case_number.startswith("SM-") else "FAIL", b01_case_number)
        else:
            b01_case_number = ""
            results.add("TC-B01", "FAIL", f"建立案件卡在表單頁: {ui.page.url}")

        ui.goto_new_case()
        selects = ui.page.locator("select")
        selects.nth(0).select_option(label="SM-MAINT OO超市話務系統維護案")
        ui.page.wait_for_timeout(700)
        body = ui.body_text()
        module_options = selects.nth(2).evaluate("el => [...el.options].map(o => o.text)")
        results.add("TC-B02", "PASS" if "OO超市股份有限公司" in body and "話務通話模組" in module_options else "FAIL", "project autofill")

        ui.goto_new_case()
        create_buttons = ui.page.locator("button")
        create_disabled = False
        for index in range(create_buttons.count()):
            if create_buttons.nth(index).inner_text().strip() == "建立案件":
                create_disabled = create_buttons.nth(index).is_disabled()
                break
        results.add("TC-B03", "PASS" if create_disabled else "FAIL", f"create_disabled={create_disabled}")

        ui.goto_new_case()
        file_input = ui.page.locator('input[type="file"]')
        file_input.set_input_files(str(large_file))
        ui.page.wait_for_timeout(1000)
        body_large = ui.body_text()
        file_input.set_input_files(str(invalid_file))
        ui.page.wait_for_timeout(1000)
        body_invalid = ui.body_text()
        blocked = ("20MB" in body_large or "20MB" in body_invalid or "不支援" in body_invalid or ".exe" in body_invalid)
        results.add("TC-B04", "PASS" if blocked else "FAIL", "upload validation")

        ui.goto_new_case()
        ui.fill_new_case_form(
            reporter_name="草稿測試",
            phone="0987654321",
            email="draft@example.com",
            case_type="障礙調查",
            category_label="日常操作異常",
            module_label="話務通話模組",
            description="TC-B05 草稿內容",
        )
        ui.click_button("儲存草稿")
        ui.page.wait_for_timeout(1000)
        ui.page.reload(wait_until="networkidle")
        preserved = "草稿測試" in ui.body_text() or ui.page.locator('input[placeholder="客戶端聯絡人姓名"]').input_value() == "草稿測試"
        ui.click_button("清除草稿紀錄")
        ui.page.wait_for_timeout(1000)
        cleared = ui.page.locator('input[placeholder="客戶端聯絡人姓名"]').input_value() == ""
        results.add("TC-B05", "PASS" if preserved and cleared else "FAIL", f"preserved={preserved} cleared={cleared}")

        ui.goto_dashboard()
        before = ui.body_text()
        ui.goto_new_case()
        ui.fill_new_case_form(
            reporter_name="通知測試",
            phone="0900111222",
            email="notice@example.com",
            case_type="障礙調查",
            category_label="日常操作異常",
            module_label="話務通話模組",
            description="TC-B06 通知測試",
        )
        ui.click_button("建立案件")
        ui.page.wait_for_timeout(8000)
        if "/cases/" in ui.page.url and not ui.page.url.endswith("/cases/new"):
            slug = ui.page.url.rstrip("/").split("/")[-1]
            created_number = api.request("GET", f"/cases/{slug}", "luffy").json()["data"]["case_number"]
            ui.goto_dashboard()
            after = ui.body_text()
            results.add("TC-B06", "PASS" if created_number in after and before != after else "FAIL", created_number)
        else:
            results.add("TC-B06", "FAIL", "建案後通知驗證無法完成：畫面停在建立中")

        history_source = api.create_case("TC-B07 歷史引用來源")
        api.assign(history_source["id"], ["nami"])
        api.add_log(history_source["id"], "nami")
        api.complete(history_source["id"], "nami")
        api.close(history_source["id"])
        ui.goto_new_case()
        ui.click_button("從歷史案件引用")
        ui.page.wait_for_timeout(1000)
        ui.page.get_by_text(history_source["case_number"], exact=True).click()
        ui.click_button("整案欄位引用")
        ui.page.wait_for_timeout(1500)
        body = ui.body_text()
        results.add("TC-B07", "PASS" if history_source["case_number"] in body and "已引用" in body else "FAIL", history_source["case_number"])

        c01 = api.create_case("TC-C01 單一派工")
        ui.goto_case(c01["short_id"])
        ui.click_button("轉派專案成員")
        ui.page.locator('input[type="checkbox"]').nth(0).check()
        ui.page.locator("textarea").fill("請優先釐清")
        ui.click_button("確認派工")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-C01", "PASS" if "已派工" in body and "娜美" in body else "FAIL", c01["case_number"])

        ui.logout()
        ui.login("nami")
        ui.goto_dashboard()
        body = ui.body_text()
        results.add("TC-C02", "PASS" if c01["case_number"] in body else "FAIL", c01["case_number"])

        ui.logout()
        ui.login("luffy")
        c03 = api.create_case("TC-C03 多人派工")
        ui.goto_case(c03["short_id"])
        ui.click_button("轉派專案成員")
        ui.page.locator('input[type="checkbox"]').nth(0).check()
        ui.page.locator('input[type="checkbox"]').nth(1).check()
        ui.click_button("確認派工")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-C03", "PASS" if "娜美" in body and "宇智波佐助" in body else "FAIL", c03["case_number"])

        c04 = api.create_case("TC-C04 改派")
        api.assign(c04["id"], ["nami"])
        ui.goto_case(c04["short_id"])
        ui.click_button("轉派專案成員")
        if ui.page.locator('input[type="checkbox"]').nth(0).is_checked():
            ui.page.locator('input[type="checkbox"]').nth(0).uncheck()
        ui.page.locator('input[type="checkbox"]').nth(1).check()
        ui.click_button("確認派工")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-C04", "PASS" if "宇智波佐助" in body and "歷史派工紀錄" in body else "FAIL", c04["case_number"])

        ui.logout()
        ui.login("sasuke")
        ui.goto_case(c04["short_id"])
        results.add("TC-C05", "PASS" if "轉派專案成員" not in ui.body_text() else "FAIL", c04["case_number"])

        body_new = ui.body_text()
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("1")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("TC-C06 新承辦人可新增紀錄")
        textareas.nth(1).fill("ok")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        new_can_operate = "處理中" in ui.body_text()

        ui.logout()
        ui.login("nami")
        ui.goto_case(c04["short_id"])
        denied_old = "找不到此案件" in ui.body_text() or "無權存取" in ui.body_text()
        results.add("TC-C06", "PASS" if new_can_operate and denied_old else "FAIL", c04["case_number"])

        d01 = api.create_case("TC-D01 處理紀錄")
        api.assign(d01["id"], ["nami"])
        ui.goto_case(d01["short_id"])
        if "/login" in ui.page.url:
            ui.login("nami")
            ui.goto_case(d01["short_id"])
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("2")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("檢查 CTI log")
        textareas.nth(1).fill("待觀察")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-D01", "PASS" if "處理中" in body and "總工時 2 hr" in body else "FAIL", d01["case_number"])

        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("1.5")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("第二筆紀錄")
        textareas.nth(1).fill("第二筆結果")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-D02", "PASS" if "總工時 3.5 hr" in body and "共 2 筆紀錄" in body else "FAIL", d01["case_number"])

        ui.click_button("回報完工")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-D03", "PASS" if "已完工" in ui.body_text() else "FAIL", d01["case_number"])

        d04 = api.create_case("TC-D04 退回再完工")
        api.assign(d04["id"], ["nami"])
        api.add_log(d04["id"], "nami")
        api.complete(d04["id"], "nami")
        api.return_case(d04["id"])
        ui.goto_case(d04["short_id"])
        no_complete_before = "回報完工" not in ui.body_text()
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("1")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("退回後補紀錄")
        textareas.nth(1).fill("已修復")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        can_complete_after = "回報完工" in ui.body_text()
        if can_complete_after:
            ui.click_button("回報完工")
            ui.page.wait_for_timeout(500)
            ui.click_button("確認")
            ui.page.wait_for_load_state("networkidle")
        results.add("TC-D04", "PASS" if no_complete_before and can_complete_after and "已完工" in ui.body_text() else "FAIL", d04["case_number"])

        results.add("TC-D05", "PASS" if "刪除" not in ui.body_text() else "FAIL", d04["case_number"])

        d06 = api.create_case("TC-D06 多人派工完工")
        api.assign(d06["id"], ["nami", "sasuke"])
        ui.goto_case(d06["short_id"])
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("1")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("首位完工前紀錄")
        textareas.nth(1).fill("ok")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        ui.click_button("回報完工")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        ui.logout()
        ui.login("sasuke")
        ui.goto_case(d06["short_id"])
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("0.5")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("第二位 SE 補紀錄")
        textareas.nth(1).fill("不重發")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-D06", "PASS" if "已完工" in ui.body_text() else "FAIL", d06["case_number"])

        source = api.create_case("TC-D07 引用歷史紀錄來源")
        api.assign(source["id"], ["nami"])
        api.add_log(source["id"], "nami", handling_method="歷史處理方式", result="歷史處理結果")
        api.complete(source["id"], "nami")
        api.close(source["id"])
        d07 = api.create_case("TC-D07 目標案件")
        api.assign(d07["id"], ["nami"])
        ui.goto_case(d07["short_id"])
        ui.click_button("新增處理紀錄")
        ui.click_button("引用歷史案件")
        ui.page.wait_for_timeout(1000)
        ui.page.get_by_text(source["case_number"], exact=True).click()
        ui.page.get_by_text("引用此紀錄", exact=False).first.click()
        ui.page.wait_for_timeout(1000)
        textareas = ui.page.locator("textarea")
        imported = "歷史處理方式" in textareas.nth(0).input_value() and "歷史處理結果" in textareas.nth(1).input_value()
        results.add("TC-D07", "PASS" if imported else "FAIL", source["case_number"])

        d08 = api.create_case("TC-D08 工時評估案件", case_type="EVALUATION")
        api.assign(d08["id"], ["nami"])
        ui.goto_case(d08["short_id"])
        body = ui.body_text()
        results.add("TC-D08", "PASS" if "評估回覆工時" in body or "工時評估" in body else "FAIL", d08["case_number"])

        d09 = api.create_case("TC-D09 PM 自行處理")
        ui.logout()
        ui.login("luffy")
        ui.goto_case(d09["short_id"])
        has_add_log = "新增處理紀錄" in ui.body_text()
        ui.click_button("新增處理紀錄")
        ui.page.locator('input[type="text"]').last.fill("1")
        textareas = ui.page.locator("textarea")
        textareas.nth(0).fill("PM 自行處理")
        textareas.nth(1).fill("ok")
        ui.click_button("✓ 送出")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-D09", "PASS" if has_add_log and "處理中" in ui.body_text() else "FAIL", d09["case_number"])

        has_complete = "回報完工" in ui.body_text()
        ui.click_button("回報完工")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-D10", "PASS" if has_complete and "已完工" in ui.body_text() else "FAIL", d09["case_number"])

        e01 = api.create_case("TC-E01 退回")
        api.assign(e01["id"], ["nami"])
        api.add_log(e01["id"], "nami")
        api.complete(e01["id"], "nami")
        ui.goto_case(e01["short_id"])
        ui.click_button("退回")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-E01", "PASS" if "已退回" in ui.body_text() else "FAIL", e01["case_number"])

        e02 = api.create_case("TC-E02 結案")
        api.assign(e02["id"], ["nami"])
        api.add_log(e02["id"], "nami")
        api.complete(e02["id"], "nami")
        ui.goto_case(e02["short_id"])
        ui.click_button("確認結案")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-E02", "PASS" if "已結案" in body and "重開" in body else "FAIL", e02["case_number"])

        e03 = api.create_case("TC-E03 取消")
        ui.goto_case(e03["short_id"])
        ui.click_button("取消")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-E03", "PASS" if "已取消" in body and "重開" in body else "FAIL", e03["case_number"])

        ui.click_button("重開")
        ui.page.wait_for_timeout(500)
        ui.click_button("確認")
        ui.page.wait_for_load_state("networkidle")
        body = ui.body_text()
        results.add("TC-E04", "PASS" if "待處理" in body and ui.newest_case_number_from_detail() != e03["case_number"] else "FAIL", e03["case_number"])

        e05 = api.create_case("TC-E05 回覆客戶")
        ui.goto_case(e05["short_id"])
        ui.click_button("回覆客戶")
        ui.page.locator("textarea").fill("已重新開案跟進")
        ui.click_button("送出")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-E05", "PASS" if "已重新開案跟進" in ui.body_text() else "FAIL", e05["case_number"])

        e06 = api.create_case("TC-E06 PM 直接回覆")
        ui.goto_case(e06["short_id"])
        ui.click_button("回覆客戶")
        ui.page.locator("textarea").fill("測試PM立案後直接回覆")
        ui.click_button("送出")
        ui.page.wait_for_load_state("networkidle")
        results.add("TC-E06", "PASS" if "測試PM立案後直接回覆" in ui.body_text() else "FAIL", e06["case_number"])

        results.add("TC-F01", "PASS" if "轉派專案成員" not in ui.body_text() else "FAIL", e02["case_number"])

        ui.logout()
        ui.page.goto(f"{BASE_UI}/dashboard", wait_until="networkidle")
        results.add("TC-F06", "PASS" if "/login" in ui.page.url else "FAIL", ui.page.url)

        ui.login("luffy")
        ui.page.goto(f"{BASE_UI}/cases/ZZZZZZZZ", wait_until="networkidle")
        results.add("TC-F07", "PASS" if "找不到此案件" in ui.body_text() else "FAIL", ui.page.url)

        ui.goto_dashboard()
        body = ui.body_text()
        dashboard_ok = all(label in body for label in ["待處理", "已派工", "處理中", "已完工待確認", "我的待辦案件"])
        results.add("TC-G01", "PASS" if dashboard_ok else "FAIL", "dashboard")

        ui.goto_cases()
        ui.page.locator('input[placeholder*="搜尋"]').fill("SM-202606")
        ui.page.wait_for_timeout(1000)
        results.add("TC-G02", "PASS" if "案件列表" in ui.body_text() else "FAIL", "list")

        g03_case = api.create_case("TC-G03 僅給娜美")
        api.assign(g03_case["id"], ["nami"])
        ui.logout()
        ui.login("nami")
        ui.goto_cases()
        body = ui.body_text()
        results.add("TC-G03", "PASS" if g03_case["case_number"] in body else "FAIL", g03_case["case_number"])

        g04_case = api.create_case("TC-G04 待確認標示")
        api.assign(g04_case["id"], ["nami"])
        api.add_log(g04_case["id"], "nami")
        api.complete(g04_case["id"], "nami")
        ui.logout()
        ui.login("luffy")
        ui.goto_cases()
        body = ui.body_text()
        results.add("TC-G04", "PASS" if g04_case["case_number"] in body and "待確認" in body else "FAIL", g04_case["case_number"])

        results.add("TC-H01", "SKIP", "前端情境無法直接操作，屬 API 權限驗證")

        h02_case = api.create_case("TC-H02 取消權限")
        api.assign(h02_case["id"], ["sasuke"])
        ui.logout()
        ui.login("sasuke")
        ui.goto_case(h02_case["short_id"])
        results.add("TC-H02", "FAIL" if "取消" in ui.body_text() else "PASS", "已知缺陷：SE 仍看得到取消")

        ui.logout()
        ui.login("conan")
        ui.goto_case(unassigned["short_id"])
        results.add("TC-H03", "PASS" if "找不到此案件" in ui.body_text() or "無權存取" in ui.body_text() else "FAIL", unassigned["case_number"])

        ui.logout()
        ui.login("zoro")
        ui.goto_case(unassigned["short_id"])
        results.add("TC-H04", "PASS" if "找不到此案件" in ui.body_text() or "無權存取" in ui.body_text() else "FAIL", unassigned["case_number"])

        ui.logout()
        ui.login("goku")
        ui.goto_case(unassigned["short_id"])
        results.add("TC-H05", "PASS" if "找不到此案件" in ui.body_text() or "無權存取" in ui.body_text() else "FAIL", unassigned["case_number"])

        ui.logout()
        ui.login("sasuke")
        ui.goto_case(completed["short_id"])
        results.add("TC-H06", "PASS" if "確認結案" not in ui.body_text() else "FAIL", completed["case_number"])

    except PlaywrightTimeoutError as exc:
        results.add("RUNNER", "FAIL", f"timeout: {exc}")
        raise
    except Exception as exc:
        results.add("RUNNER", "FAIL", str(exc))
        raise
    finally:
        results.write()
        ui.close()


if __name__ == "__main__":
    main()
