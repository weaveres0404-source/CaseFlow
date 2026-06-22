import json
from datetime import date
from pathlib import Path

import requests
from playwright.sync_api import sync_playwright

BASE_UI = "https://caseflow-test.sld-lwd.com"
BASE_API = f"{BASE_UI}/api/v1"
PASSWORD = "@sld123456"
CHROME_PATH = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
RESULT_PATH = Path(__file__).with_name("manual_ui_blue_results.json")


def load_results():
    if RESULT_PATH.exists():
        return json.loads(RESULT_PATH.read_text(encoding="utf-8"))
    return []


def save_results(items):
    RESULT_PATH.write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8")


def upsert(items, case_id, status, note):
    safe_note = str(note).encode("cp950", errors="replace").decode("cp950", errors="replace")
    print(f"[{status}] {case_id} - {safe_note}")
    for item in items:
        if item["id"] == case_id:
            item["status"] = status
            item["note"] = note
            return
    items.append({"id": case_id, "status": status, "note": note})


class Api:
    def __init__(self):
        self.session = requests.Session()
        self.tokens = {}
        for user in ["luffy", "nami", "sasuke", "conan", "zoro", "goku", "admin"]:
            self.tokens[user] = self.login(user)
        self.meta = self.request("GET", "/meta/dropdowns", "luffy").json()["data"]
        self.users = {user["username"]: user["id"] for user in self.meta["users"]}
        self.project = next(project for project in self.meta["projects"] if project.get("code") == "SM-MAINT")
        self.category = next(
            category
            for category in self.meta["categories"]
            if category.get("case_type_filter") == "REPAIR" and self.project["id"] in category.get("project_ids", [])
        )
        self.eval_category = next(
            category
            for category in self.meta["categories"]
            if category.get("case_type_filter") == "EVALUATION" and self.project["id"] in category.get("project_ids", [])
        )
        self.module = next(module for module in self.meta["modules"] if module.get("project_id") == self.project["id"])

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

    def create_case(self, description, case_type="REPAIR", username="luffy"):
        category = self.eval_category if case_type == "EVALUATION" else self.category
        response = self.request(
            "POST",
            "/cases",
            username,
            json={
                "project_id": self.project["id"],
                "customer_id": self.project["customer_id"],
                "category_id": category["id"],
                "module_id": self.module["id"],
                "case_type": case_type,
                "priority": "MEDIUM",
                "reporter_name": "UI Continue",
                "reporter_phone": "0900000000",
                "reporter_email": "ui-continue@test.local",
                "description": description,
            },
        )
        response.raise_for_status()
        return response.json()["data"]

    def assign(self, case_id, usernames, by="luffy"):
        ids = [self.users[name] for name in usernames]
        self.request(
            "POST",
            f"/cases/{case_id}/assign",
            by,
            json={"se_user_ids": ids, "primary_se_user_id": ids[0]},
        ).raise_for_status()

    def add_log(self, case_id, username, hours=1, method="api prep", result="ok"):
        self.request(
            "POST",
            f"/cases/{case_id}/logs",
            username,
            json={
                "handling_method": method,
                "result": result,
                "hours_spent": hours,
                "log_date": str(date.today()),
            },
        ).raise_for_status()

    def complete(self, case_id, username):
        self.request("POST", f"/cases/{case_id}/complete", username, json={}).raise_for_status()

    def close(self, case_id, username="luffy"):
        self.request("POST", f"/cases/{case_id}/close", username, json={"close_remark": "close"}).raise_for_status()

    def cancel(self, case_id, username="luffy"):
        self.request("POST", f"/cases/{case_id}/cancel", username, json={"reason": "cancel"}).raise_for_status()

    def return_case(self, case_id, username="luffy"):
        self.request("POST", f"/cases/{case_id}/return", username, json={"reason": "return"}).raise_for_status()


class Ui:
    def __init__(self):
        self.pw = sync_playwright().start()
        self.browser = self.pw.chromium.launch(headless=True, executable_path=CHROME_PATH)
        self.page = self.browser.new_page(viewport={"width": 1600, "height": 2600})

    def close(self):
        self.browser.close()
        self.pw.stop()

    def login(self, username):
        self.page.goto(f"{BASE_UI}/login", wait_until="networkidle")
        self.page.fill('input[name="account"]', username)
        self.page.fill('input[name="password"]', PASSWORD)
        self.page.locator("button").last.click()
        self.page.wait_for_timeout(5000)

    def goto_case(self, slug):
        self.page.goto(f"{BASE_UI}/cases/{slug}", wait_until="networkidle")

    def buttons(self):
        return self.page.locator("button").all_inner_texts()

    def body(self):
        return self.page.locator("body").inner_text()

    def click_index(self, idx):
        self.page.locator("button").nth(idx).click()
        self.page.wait_for_timeout(800)


def main():
    results = load_results()
    api = Api()
    ui = Ui()
    try:
        ui.login("luffy")

        d03 = api.create_case("TC-D03 UI complete")
        api.assign(d03["id"], ["nami"])
        api.add_log(d03["id"], "nami")
        ui.goto_case(d03["short_id"])
        ui.click_index(5)
        body = ui.body()
        upsert(results, "TC-D03", "PASS" if "確認此案件已完工？" in body and "確認操作" in body else "FAIL", d03["case_number"])

        d04 = api.create_case("TC-D04 returned no complete")
        api.assign(d04["id"], ["nami"])
        api.add_log(d04["id"], "nami")
        api.complete(d04["id"], "nami")
        api.return_case(d04["id"])
        ui.goto_case(d04["short_id"])
        body = ui.body()
        upsert(results, "TC-D04", "FAIL" if "新增處理紀錄" in body and "回報完工" not in body else "PASS", d04["case_number"])

        upsert(results, "TC-D05", "PASS" if "刪除" not in body else "FAIL", d04["case_number"])

        d06 = api.create_case("TC-D06 complete then extra log")
        api.assign(d06["id"], ["nami", "sasuke"])
        api.add_log(d06["id"], "nami")
        api.complete(d06["id"], "nami")
        ui.goto_case(d06["short_id"])
        body = ui.body()
        upsert(results, "TC-D06", "FAIL" if "新增處理紀錄" in body else "PASS", d06["case_number"])

        d07 = api.create_case("TC-D07 ref history target")
        api.assign(d07["id"], ["nami"])
        ui.goto_case(d07["short_id"])
        body = ui.body()
        upsert(results, "TC-D07", "FAIL" if "新增處理紀錄" in body else "PASS", d07["case_number"])

        d08 = api.create_case("TC-D08 evaluation UI", case_type="EVALUATION")
        api.assign(d08["id"], ["nami"])
        ui.goto_case(d08["short_id"])
        body = ui.body()
        upsert(results, "TC-D08", "FAIL" if "新增處理紀錄" in body else "PASS", d08["case_number"])

        d09 = api.create_case("TC-D09 PM add log")
        ui.goto_case(d09["short_id"])
        body = ui.body()
        upsert(results, "TC-D09", "FAIL" if "新增處理紀錄" in body else "PASS", d09["case_number"])

        d10 = api.create_case("TC-D10 PM complete")
        api.add_log(d10["id"], "luffy")
        ui.goto_case(d10["short_id"])
        body = ui.body()
        if "回報完工" in body:
            labels = ui.buttons()
            if "回報完工" in labels:
                ui.click_index(labels.index("回報完工"))
                body = ui.body()
        upsert(results, "TC-D10", "PASS" if "確認此案件已完工？" in body else "FAIL", d10["case_number"])

        e01 = api.create_case("TC-E01 return")
        api.assign(e01["id"], ["nami"])
        api.add_log(e01["id"], "nami")
        api.complete(e01["id"], "nami")
        ui.goto_case(e01["short_id"])
        ui.click_index(4)
        upsert(results, "TC-E01", "PASS" if "確認退回此案件？" in ui.body() else "FAIL", e01["case_number"])

        e02 = api.create_case("TC-E02 close")
        api.assign(e02["id"], ["nami"])
        api.add_log(e02["id"], "nami")
        api.complete(e02["id"], "nami")
        ui.goto_case(e02["short_id"])
        ui.click_index(3)
        upsert(results, "TC-E02", "PASS" if "確認結案？" in ui.body() else "FAIL", e02["case_number"])

        e03 = api.create_case("TC-E03 cancel")
        ui.goto_case(e03["short_id"])
        ui.click_index(6)
        upsert(results, "TC-E03", "PASS" if "確認取消此案件？此操作不可逆" in ui.body() else "FAIL", e03["case_number"])

        e04 = api.create_case("TC-E04 reopen")
        api.cancel(e04["id"])
        ui.goto_case(e04["short_id"])
        labels = ui.buttons()
        if "重開" in labels:
            ui.click_index(labels.index("重開"))
            body = ui.body()
            status = "PASS" if "以舊案為範本建立新案件" in body or "確認操作" in body else "FAIL"
        else:
            body = ui.body()
            status = "FAIL"
        upsert(results, "TC-E04", status, e04["case_number"])

        e05 = api.create_case("TC-E05 reply")
        ui.goto_case(e05["short_id"])
        ui.click_index(5)
        body = ui.body()
        upsert(results, "TC-E05", "PASS" if "回覆內容 *" in body and "送出" in body else "FAIL", e05["case_number"])

        e06 = api.create_case("TC-E06 reply without assign")
        ui.goto_case(e06["short_id"])
        ui.click_index(5)
        body = ui.body()
        upsert(results, "TC-E06", "PASS" if "回覆內容 *" in body and "送出" in body else "FAIL", e06["case_number"])

        f01 = api.create_case("TC-F01 closed no assign")
        api.assign(f01["id"], ["nami"])
        api.add_log(f01["id"], "nami")
        api.complete(f01["id"], "nami")
        api.close(f01["id"])
        ui.goto_case(f01["short_id"])
        upsert(results, "TC-F01", "PASS" if "轉派專案成員" not in ui.body() else "FAIL", f01["case_number"])

        f05 = f01
        ui.login("nami")
        ui.goto_case(f05["short_id"])
        body = ui.body()
        upsert(results, "TC-F05", "PASS" if "找不到此案件" in body or "無權存取" in body or "已結案" in body else "FAIL", f05["case_number"])

        ui.page.goto(f"{BASE_UI}/cases/ZZZZZZZZ", wait_until="networkidle")
        upsert(results, "TC-F07", "PASS" if "找不到此案件" in ui.body() else "FAIL", ui.page.url)

        ui.login("luffy")
        g04 = api.create_case("TC-G04 completed badge")
        api.assign(g04["id"], ["nami"])
        api.add_log(g04["id"], "nami")
        api.complete(g04["id"], "nami")
        ui.page.goto(f"{BASE_UI}/cases", wait_until="networkidle")
        body = ui.body()
        upsert(results, "TC-G04", "PASS" if g04["case_number"] in body and "待確認" in body else "FAIL", g04["case_number"])

        h06 = api.create_case("TC-H06 se no close")
        api.assign(h06["id"], ["sasuke"])
        api.add_log(h06["id"], "sasuke")
        api.complete(h06["id"], "sasuke")
        ui.login("sasuke")
        ui.goto_case(h06["short_id"])
        upsert(results, "TC-H06", "PASS" if "確認結案" not in ui.body() else "FAIL", h06["case_number"])

        upsert(results, "RUNNER_CONTINUE", "PASS", "D03~H06 continuation finished")
    finally:
        save_results(results)
        ui.close()


if __name__ == "__main__":
    main()
