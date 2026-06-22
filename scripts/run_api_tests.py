"""
CaseFlow 自動化 API 測試 runner
依 ④ 測試情境 K/L/M 欄執行，並把結果寫回 scenarios_result.json
"""
import requests, json, sys, time, traceback
from datetime import date

BASE = "https://caseflow-test.sld-lwd.com/api/v1"
PWD = "@sld123456"
USERS = ["luffy", "zoro", "conan", "nami", "sasuke", "goku", "admin"]

session = requests.Session()
session.headers.update({"Accept": "application/json"})

# -------------- 共用 ----------------
def login(username, password=PWD):
    r = session.post(f"{BASE}/auth/login", json={"username": username, "password": password}, timeout=30)
    return r

def auth(token):
    return {"Authorization": f"Bearer {token}"}

def call(method, url, token=None, **kw):
    h = kw.pop("headers", {}) or {}
    if token: h.update(auth(token))
    r = session.request(method, url, headers=h, timeout=30, **kw)
    return r

# -------------- 主程序 ----------------
def main():
    results = []  # (tc_id, status PASS/FAIL/SKIP, note)
    tokens = {}
    user_info = {}

    # ---- 認證 / TC-A 系列 ----
    # TC-A01 PM login
    r = login("luffy")
    pass_ = r.status_code == 200 and r.json().get("success") and r.json().get("data", {}).get("access_token")
    results.append(("TC-A01", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    if pass_:
        tokens["luffy"] = r.json()["data"]["access_token"]
        user_info["luffy"] = r.json()["data"]["user"]

    # TC-A02 SE login
    r = login("nami")
    pass_ = r.status_code == 200 and r.json().get("success")
    results.append(("TC-A02", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code}"))
    if pass_:
        tokens["nami"] = r.json()["data"]["access_token"]
        user_info["nami"] = r.json()["data"]["user"]

    # 其他 user 也先登入備用
    for u in ["zoro", "conan", "sasuke", "goku", "admin"]:
        r = login(u)
        if r.status_code == 200 and r.json().get("success"):
            tokens[u] = r.json()["data"]["access_token"]
            user_info[u] = r.json()["data"]["user"]
        else:
            results.append((f"login-{u}", "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    # TC-A03 wrong password
    r = session.post(f"{BASE}/auth/login", json={"username": "luffy", "password": "wrongpwd"}, timeout=30)
    body = {}
    try: body = r.json()
    except: pass
    pass_ = body.get("success") is False
    results.append(("TC-A03", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    # TC-A04 first-login (需手動建帳號) — SKIP
    results.append(("TC-A04", "SKIP", "需 DBA 建 must_change_password=TRUE 帳號"))

    # TC-A05 SE 無新增案件入口 — UI only
    results.append(("TC-A05", "SKIP", "UI-only"))

    # ---- meta dropdowns 取得元資料供後續 ----
    r = call("GET", f"{BASE}/meta/dropdowns", token=tokens.get("luffy"))
    meta = r.json().get("data", {}) if r.status_code == 200 else {}
    projects = meta.get("projects", [])
    categories = meta.get("categories", [])
    case_types = meta.get("case_types", [])
    modules = meta.get("modules", [])
    project_members = meta.get("project_members", [])
    users = meta.get("users", [])

    def find_project(code_kw):
        for p in projects:
            code = (p.get("project_code") or p.get("code") or "")
            if code_kw in code: return p
        return None

    p1 = find_project("SM-MAINT") or (projects[0] if projects else None)
    p2 = find_project("TC-CRM")

    if not p1:
        results.append(("setup-projects", "FAIL", "找不到專案 SM-MAINT"))
        write_results(results); return

    # 取得 user id
    def uid(uname):
        info = user_info.get(uname) or {}
        return info.get("user_id") or info.get("id")

    # 取得對應 project 的 case_type / category / module
    def first_for_project(items, pid):
        # case_types / categories 有 project_ids array；無則視為共用
        cands = [t for t in items if not t.get("project_ids") or pid in t["project_ids"]]
        return cands[0] if cands else (items[0] if items else None)

    ct_default = first_for_project(case_types, p1.get("id") or p1.get("project_id"))
    # 找 障礙調查
    ct_repair = next((t for t in case_types if (t.get("name") or "").strip() == "障礙調查"), ct_default)
    cat_default = first_for_project(categories, p1.get("id"))
    cat_daily = next((c for c in categories if "日常操作" in (c.get("name") or "")), cat_default)
    mod_default = next((m for m in modules if (m.get("project_id") == (p1.get("id") or p1.get("project_id")))), None)

    pid1 = p1.get("id") or p1.get("project_id")

    # 工時評估 case type（給 TC-D08）
    ct_eval = next((t for t in case_types if t.get("code") == "EVALUATION" or "工時評估" in (t.get("label") or t.get("name") or "")), None)

    def build_case_body(name="自動化測試案件"):
        return {
            "project_id": pid1,
            "customer_id": p1.get("customer_id"),
            "category_id": (cat_daily or cat_default).get("id") or (cat_daily or cat_default).get("category_id"),
            "module_id": (mod_default or {}).get("id") or (mod_default or {}).get("module_id"),
            "reporter_name": "自動化測試",
            "reporter_phone": "0900000000",
            "reporter_email": "auto@test.local",
            "case_type": "REPAIR",
            "priority": "MEDIUM",
            "description": "自動化建立 " + name,
        }

    luffy_t = tokens.get("luffy")
    nami_t = tokens.get("nami")
    sasuke_t = tokens.get("sasuke")
    conan_t = tokens.get("conan")
    goku_t = tokens.get("goku")
    zoro_t = tokens.get("zoro")
    admin_t = tokens.get("admin")

    # ---- TC-B 建案 ----
    # 記錄 case_id -> short_id (slug) 供 GET 使用
    slug_map = {}
    def create_case(label):
        r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body(label))
        if r.status_code == 201:
            d = r.json()["data"]
            cid = d.get("id") or d.get("case_id")
            slug_map[cid] = d.get("short_id")
            return cid, d, r
        return None, None, r
    def slug(cid):
        return slug_map.get(cid) or str(cid)

    # TC-B01
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-B01 建立案件"))
    case_b01 = None
    if r.status_code == 201:
        d = r.json().get("data", {})
        case_b01 = d.get("id") or d.get("case_id")
        slug_map[case_b01] = d.get("short_id")
        results.append(("TC-B01", "PASS", f"201 case_id={case_b01} number={d.get('case_number')}"))
    else:
        results.append(("TC-B01", "FAIL", f"HTTP {r.status_code} body={r.text[:300]}"))

    # TC-B02 GET meta/dropdowns
    r = call("GET", f"{BASE}/meta/dropdowns", token=luffy_t)
    results.append(("TC-B02", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))

    # TC-B03 必填驗證
    r = call("POST", f"{BASE}/cases", token=luffy_t, json={"project_id": pid1})
    pass_ = r.status_code in (400, 422)
    results.append(("TC-B03", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    # TC-B04 附件大小/類型 — 簡略以 endpoint 不存在或 file missing 為主，視為 SKIP（需 multipart 大檔測）
    results.append(("TC-B04", "SKIP", "需 >20MB 二進位檔測試"))

    # TC-B05 UI only
    results.append(("TC-B05", "SKIP", "UI-only 草稿"))
    # TC-B06 通知 — 用 notifications 端點驗證
    results.append(("TC-B06", "SKIP", "通知為非同步、僅檢查 API 是否回 200"))
    # TC-B07 — 引用為前端行為，僅檢查 GET /cases 列表
    r = call("GET", f"{BASE}/cases", token=luffy_t)
    results.append(("TC-B07", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))

    # ---- TC-C 派工 ----
    nami_uid = uid("nami")
    sasuke_uid = uid("sasuke")
    conan_uid = uid("conan")

    # TC-C01 單派
    if case_b01:
        r = call("POST", f"{BASE}/cases/{case_b01}/assign", token=luffy_t,
                 json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid, "instructions": "請優先釐清"})
        results.append(("TC-C01", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-C01", "SKIP", "依賴 TC-B01"))

    results.append(("TC-C02", "SKIP", "通知"))

    # TC-C03 多派
    case_c03 = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-C03 多派"))
    if r.status_code == 201:
        case_c03 = r.json()["data"].get("id") or r.json()["data"].get("case_id")
        slug_map[case_c03] = r.json()["data"].get("short_id")
        r2 = call("POST", f"{BASE}/cases/{case_c03}/assign", token=luffy_t,
                  json={"se_user_ids": [nami_uid, sasuke_uid], "primary_se_user_id": nami_uid})
        results.append(("TC-C03", "PASS" if r2.status_code == 200 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-C03", "FAIL", f"create case failed {r.status_code}"))

    # TC-C04 改派
    if case_b01:
        r = call("POST", f"{BASE}/cases/{case_b01}/assign", token=luffy_t,
                 json={"se_user_ids": [sasuke_uid], "primary_se_user_id": sasuke_uid})
        results.append(("TC-C04", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))
    else:
        results.append(("TC-C04", "SKIP", "依賴 TC-B01"))

    # TC-C05 SE 不可派工
    if case_b01:
        r = call("POST", f"{BASE}/cases/{case_b01}/assign", token=sasuke_t,
                 json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        pass_ = r.status_code in (403,)
        results.append(("TC-C05", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-C05", "SKIP", ""))

    # TC-C06 改派後權限：B 可建 log / A 403
    if case_b01:
        # case_b01 已派給 sasuke
        rb = call("POST", f"{BASE}/cases/{case_b01}/logs", token=sasuke_t,
                  json={"handling_method": "TC-C06 B 新增", "hours_spent": 1, "log_date": str(date.today())})
        ra = call("POST", f"{BASE}/cases/{case_b01}/logs", token=nami_t,
                  json={"handling_method": "TC-C06 A 新增", "hours_spent": 1, "log_date": str(date.today())})
        pass_ = rb.status_code == 201 and ra.status_code == 403
        results.append(("TC-C06", "PASS" if pass_ else "FAIL", f"B={rb.status_code} A={ra.status_code} bodyA={ra.text[:200]}"))
    else:
        results.append(("TC-C06", "SKIP", ""))

    # ---- TC-D 處理 / 完工 ----
    # TC-D01 新增 log (未完工) — 用新案
    case_d = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-D 處理流程"))
    if r.status_code == 201:
        case_d = r.json()["data"].get("id") or r.json()["data"].get("case_id")
        slug_map[case_d] = r.json()["data"].get("short_id")
        call("POST", f"{BASE}/cases/{case_d}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        r2 = call("POST", f"{BASE}/cases/{case_d}/logs", token=nami_t,
                  json={"handling_method": "檢查 CTI log", "handling_result": "待觀察",
                        "hours_spent": 2, "log_date": str(date.today())})
        results.append(("TC-D01", "PASS" if r2.status_code == 201 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-D01", "FAIL", f"create case failed {r.status_code}"))

    # TC-D02 GET 案件，確認工時/筆數
    if case_d:
        r = call("POST", f"{BASE}/cases/{case_d}/logs", token=nami_t,
                 json={"handling_method": "second log", "hours_spent": 1.5, "log_date": str(date.today())})
        rg = call("GET", f"{BASE}/cases/{slug(case_d)}", token=nami_t)
        if rg.status_code == 200:
            d = rg.json().get("data", {})
            logs = d.get("logs", [])
            total = (d.get("summary") or {}).get("total_hours")
            pass_ = len(logs) >= 2
            results.append(("TC-D02", "PASS" if pass_ else "FAIL", f"logs={len(logs)} total_hours={total}"))
        else:
            results.append(("TC-D02", "FAIL", f"HTTP {rg.status_code}"))
    else:
        results.append(("TC-D02", "SKIP", ""))

    # TC-D03 SE 回報完工
    if case_d:
        r = call("POST", f"{BASE}/cases/{case_d}/complete", token=nami_t)
        results.append(("TC-D03", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-D03", "SKIP", ""))

    # TC-D04 已退回(35) → 補 log → 完工
    case_d4 = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-D04 退回流程"))
    if r.status_code == 201:
        case_d4 = r.json()["data"].get("id") or r.json()["data"].get("case_id")
        call("POST", f"{BASE}/cases/{case_d4}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        call("POST", f"{BASE}/cases/{case_d4}/logs", token=nami_t,
             json={"handling_method": "first", "hours_spent": 1, "log_date": str(date.today())})
        call("POST", f"{BASE}/cases/{case_d4}/complete", token=nami_t)
        # PM 退回
        call("POST", f"{BASE}/cases/{case_d4}/return", token=luffy_t, json={"reason": "TC-D04 退回"})
        # SE 補 log
        r1 = call("POST", f"{BASE}/cases/{case_d4}/logs", token=nami_t,
                  json={"handling_method": "修復", "hours_spent": 1, "log_date": str(date.today())})
        r2 = call("POST", f"{BASE}/cases/{case_d4}/complete", token=nami_t)
        pass_ = r1.status_code == 201 and r2.status_code == 200
        results.append(("TC-D04", "PASS" if pass_ else "FAIL",
                        f"log={r1.status_code} complete={r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-D04", "FAIL", f"create case failed {r.status_code}"))

    # TC-D05 無 DELETE log
    results.append(("TC-D05", "SKIP", "API 文件規範無 DELETE"))

    # TC-D06 多人完工通知 — SKIP（通知）
    results.append(("TC-D06", "SKIP", "通知"))

    # TC-D07 處理紀錄引用歷史 — 前端行為
    r = call("GET", f"{BASE}/cases?status=50", token=nami_t)
    results.append(("TC-D07", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))

    # TC-D09 PM 自行新增處理紀錄（未派工案件，立案人 luffy）
    # K/L/M 為空（前端重點），但仍用 API 驗後端允許
    case_d9 = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-D09 PM自行新增紀錄"))
    if r.status_code == 201:
        case_d9 = r.json()["data"].get("id")
        # 不指派 SE，直接以 PM 新增處理紀錄
        r2 = call("POST", f"{BASE}/cases/{case_d9}/logs", token=luffy_t,
                  json={"handling_method": "PM自行處理測試", "hours_spent": 1, "log_date": str(date.today())})
        results.append(("TC-D09", "PASS" if r2.status_code == 201 else "FAIL",
                        f"PM add log HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-D09", "FAIL", f"create case failed {r.status_code}"))

    # TC-D10 PM（立案人）自行回報完工
    # 承 TC-D09；案件狀態應已到 30（處理中）
    if case_d9:
        r = call("POST", f"{BASE}/cases/{case_d9}/complete", token=luffy_t)
        results.append(("TC-D10", "PASS" if r.status_code == 200 else "FAIL",
                        f"PM complete HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-D10", "SKIP", "依賴 TC-D09"))

    # TC-D08 工時評估案 — 雙工時欄
    # 找一個能對應 EVALUATION 的 category；EVALUATION case_type 在 meta 中對應的 id
    eval_ct_id = (ct_eval or {}).get("id") or (ct_eval or {}).get("case_type_id")
    eval_cat = None
    for c in categories:
        if not c.get("project_ids") or pid1 in c["project_ids"]:
            ids = c.get("case_type_ids") or []
            if not ids or (eval_ct_id and eval_ct_id in ids):
                eval_cat = c; break
    body = build_case_body("TC-D08 工時評估")
    body["case_type"] = "EVALUATION"
    if eval_cat:
        body["category_id"] = eval_cat.get("id") or eval_cat.get("category_id")
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=body)
    if r.status_code == 201:
        cid = r.json()["data"].get("id")
        call("POST", f"{BASE}/cases/{cid}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        # 合法
        r1 = call("POST", f"{BASE}/cases/{cid}/logs", token=nami_t,
                  json={"handling_method": "ok", "hours_spent": 2.5,
                        "estimated_hours": 1.25, "log_date": str(date.today())})
        # 超限
        r2 = call("POST", f"{BASE}/cases/{cid}/logs", token=nami_t,
                  json={"handling_method": "too many", "hours_spent": 123456,
                        "log_date": str(date.today())})
        pass_ = r1.status_code == 201 and r2.status_code in (400, 422)
        results.append(("TC-D08", "PASS" if pass_ else "FAIL",
                       f"ok={r1.status_code} over={r2.status_code} body1={r1.text[:150]} body2={r2.text[:150]}"))
    else:
        results.append(("TC-D08", "FAIL", f"create eval case {r.status_code} body={r.text[:200]}"))

    # ---- TC-E ----
    # TC-E01 PM 退回 — 需先有 40 案件
    case_e = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-E01 退回"))
    if r.status_code == 201:
        case_e = r.json()["data"].get("id")
        call("POST", f"{BASE}/cases/{case_e}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        call("POST", f"{BASE}/cases/{case_e}/logs", token=nami_t,
             json={"handling_method": "init", "hours_spent": 1, "log_date": str(date.today())})
        call("POST", f"{BASE}/cases/{case_e}/complete", token=nami_t)
        r2 = call("POST", f"{BASE}/cases/{case_e}/return", token=luffy_t, json={"reason": "TC-E01"})
        results.append(("TC-E01", "PASS" if r2.status_code == 200 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-E01", "FAIL", f"create failed {r.status_code}"))

    # TC-E02 PM 結案 — 需 40 案件
    case_e2 = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-E02 結案"))
    if r.status_code == 201:
        case_e2 = r.json()["data"].get("id")
        call("POST", f"{BASE}/cases/{case_e2}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        call("POST", f"{BASE}/cases/{case_e2}/logs", token=nami_t,
             json={"handling_method": "init", "hours_spent": 1, "log_date": str(date.today())})
        call("POST", f"{BASE}/cases/{case_e2}/complete", token=nami_t)
        r2 = call("POST", f"{BASE}/cases/{case_e2}/close", token=luffy_t)
        results.append(("TC-E02", "PASS" if r2.status_code == 200 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-E02", "FAIL", f"create failed {r.status_code}"))

    # TC-E03 PM 取消
    case_e3 = None
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-E03 取消"))
    if r.status_code == 201:
        case_e3 = r.json()["data"].get("id")
        r2 = call("POST", f"{BASE}/cases/{case_e3}/cancel", token=luffy_t, json={"reason": "TC-E03 test"})
        results.append(("TC-E03", "PASS" if r2.status_code == 200 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:200]}"))
    else:
        results.append(("TC-E03", "FAIL", f"create failed {r.status_code}"))

    # TC-E04 重開（從終態案產生新案）
    if case_e2:
        r = call("POST", f"{BASE}/cases/{case_e2}/reopen", token=luffy_t)
        results.append(("TC-E04", "PASS" if r.status_code in (200, 201) else "FAIL",
                        f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-E04", "SKIP", ""))

    # TC-E05 PM 新增回覆客戶
    if case_b01:
        r = call("POST", f"{BASE}/cases/{case_b01}/replies", token=luffy_t,
                 json={"reply_content": "已重新開案跟進", "reply_date": str(date.today())})
        results.append(("TC-E05", "PASS" if r.status_code == 201 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-E05", "SKIP", ""))

    # TC-E06 PM 立案後未轉派即自行回覆
    r = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-E06 立案即回覆"))
    if r.status_code == 201:
        cid = r.json()["data"].get("id")
        r2 = call("POST", f"{BASE}/cases/{cid}/replies", token=luffy_t,
                  json={"reply_content": "PM 立案後直接回覆", "reply_date": str(date.today())})
        results.append(("TC-E06", "PASS" if r2.status_code == 201 else "FAIL", f"HTTP {r2.status_code} body={r2.text[:300]}"))
    else:
        results.append(("TC-E06", "FAIL", f"create failed {r.status_code}"))

    # ---- TC-F 負向 ----
    # 用結案案件 case_e2 (若已 reopen 仍為終態)
    fcase = case_e2
    if fcase:
        # F01 再派工
        r = call("POST", f"{BASE}/cases/{fcase}/assign", token=luffy_t, json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        results.append(("TC-F01", "PASS" if r.status_code == 409 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
        # F02 再完工
        r = call("POST", f"{BASE}/cases/{fcase}/complete", token=nami_t)
        results.append(("TC-F02", "PASS" if r.status_code == 409 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
        # F03 再退回
        r = call("POST", f"{BASE}/cases/{fcase}/return", token=luffy_t, json={"reason": "x"})
        results.append(("TC-F03", "PASS" if r.status_code == 409 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
        # F04 再取消
        r = call("POST", f"{BASE}/cases/{fcase}/cancel", token=luffy_t, json={"reason": "x"})
        results.append(("TC-F04", "PASS" if r.status_code == 409 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
        # F05 加 log
        r = call("POST", f"{BASE}/cases/{fcase}/logs", token=nami_t,
                 json={"handling_method": "post-close", "hours_spent": 1, "log_date": str(date.today())})
        results.append(("TC-F05", "PASS" if r.status_code == 409 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        for tc in ("TC-F01", "TC-F02", "TC-F03", "TC-F04", "TC-F05"):
            results.append((tc, "SKIP", "no terminal case"))

    # F06 未登入
    r = session.get(f"{BASE}/cases", timeout=30)
    results.append(("TC-F06", "PASS" if r.status_code == 401 else "FAIL", f"HTTP {r.status_code}"))

    # F07 不存在 case (使用長度符合但不存在的 slug)
    r = call("GET", f"{BASE}/cases/ZZZZZZZZ", token=luffy_t)
    results.append(("TC-F07", "PASS" if r.status_code == 404 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    # F08 通知批次已讀無參數
    r = call("PATCH", f"{BASE}/notifications/read", token=luffy_t, json={})
    pass_ = r.status_code in (400, 422)
    results.append(("TC-F08", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    # ---- TC-G ----
    r = call("GET", f"{BASE}/reports/dashboard", token=luffy_t)
    results.append(("TC-G01", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))

    r = call("GET", f"{BASE}/cases?status=10", token=luffy_t)
    results.append(("TC-G02", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))

    r = call("GET", f"{BASE}/cases", token=nami_t)
    if r.status_code == 200:
        body = r.json()
        data = body.get("data") if isinstance(body, dict) else body
        if isinstance(data, dict):
            items = data.get("items") or []
        else:
            items = data or []
        results.append(("TC-G03", "PASS", f"HTTP 200 items={len(items) if isinstance(items, list) else 'n/a'}"))
    else:
        results.append(("TC-G03", "FAIL", f"HTTP {r.status_code}"))

    r = call("GET", f"{BASE}/cases?status=40", token=luffy_t)
    results.append(("TC-G04", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))

    # ---- TC-A06 / A07 / A08 / A09 ----
    # 找一張案件（指派給 nami 的）
    target_case = case_b01 or case_d
    # A06 conan 同專案未指派
    if target_case and conan_t:
        r = call("GET", f"{BASE}/cases/{slug(target_case)}", token=conan_t)
        # conan 同專案，依文件應 403。若 200 表示越權成功（缺陷）
        pass_ = r.status_code == 403
        results.append(("TC-A06", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-A06", "SKIP", ""))

    if target_case and goku_t:
        r = call("GET", f"{BASE}/cases/{slug(target_case)}", token=goku_t)
        pass_ = r.status_code == 403
        results.append(("TC-A07", "PASS" if pass_ else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-A07", "SKIP", ""))

    # A08 被指派 SE 可讀
    if case_d and nami_t:
        r = call("GET", f"{BASE}/cases/{slug(case_d)}", token=nami_t)
        results.append(("TC-A08", "PASS" if r.status_code == 200 else "FAIL", f"HTTP {r.status_code}"))
    else:
        results.append(("TC-A08", "SKIP", ""))

    # A09 SE 不可結案
    if case_d and sasuke_t:
        r = call("POST", f"{BASE}/cases/{case_d}/close", token=sasuke_t)
        results.append(("TC-A09", "PASS" if r.status_code in (403,) else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-A09", "SKIP", ""))

    results.append(("TC-A10", "SKIP", "前端 localStorage"))

    # ---- TC-H ----
    # H01 conan 對超市案件呼叫派工 -> 403
    if target_case and conan_t:
        r = call("POST", f"{BASE}/cases/{target_case}/assign", token=conan_t,
                 json={"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid})
        results.append(("TC-H01", "PASS" if r.status_code == 403 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-H01", "SKIP", ""))

    # H02 非該專案 PM 取消 — sasuke 對 case_e3 ? case_e3 已 cancelled (60)，需新案
    rh2 = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-H02 取消權限"))
    if rh2.status_code == 201:
        ch2 = rh2.json()["data"].get("id")
        # 1) sasuke (該專案 SE) 嘗試取消 -> 預期 403
        r = call("POST", f"{BASE}/cases/{ch2}/cancel", token=sasuke_t, json={"reason": "x"})
        pass_ = r.status_code == 403
        results.append(("TC-H02", "PASS" if pass_ else "FAIL", f"sasuke cancel HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-H02", "SKIP", "create case failed"))

    # H03 同 A06
    if target_case and conan_t:
        r = call("GET", f"{BASE}/cases/{slug(target_case)}", token=conan_t)
        results.append(("TC-H03", "PASS" if r.status_code == 403 else "FAIL", f"HTTP {r.status_code}"))
    else:
        results.append(("TC-H03", "SKIP", ""))

    # H04 zoro 他專案 PM 載入專案1案件
    if target_case and zoro_t:
        r = call("GET", f"{BASE}/cases/{slug(target_case)}", token=zoro_t)
        results.append(("TC-H04", "PASS" if r.status_code == 403 else "FAIL", f"HTTP {r.status_code}"))
    else:
        results.append(("TC-H04", "SKIP", ""))

    # H05 goku 他專案 SE
    if target_case and goku_t:
        r = call("GET", f"{BASE}/cases/{slug(target_case)}", token=goku_t)
        results.append(("TC-H05", "PASS" if r.status_code == 403 else "FAIL", f"HTTP {r.status_code}"))
    else:
        results.append(("TC-H05", "SKIP", ""))

    # H06 專案SE 結案 (40 case + sasuke)
    case_h6 = None
    rh6 = call("POST", f"{BASE}/cases", token=luffy_t, json=build_case_body("TC-H06 SE 結案"))
    if rh6.status_code == 201:
        case_h6 = rh6.json()["data"].get("id")
        call("POST", f"{BASE}/cases/{case_h6}/assign", token=luffy_t, json={"se_user_ids": [sasuke_uid], "primary_se_user_id": sasuke_uid})
        call("POST", f"{BASE}/cases/{case_h6}/logs", token=sasuke_t,
             json={"handling_method": "init", "hours_spent": 1, "log_date": str(date.today())})
        call("POST", f"{BASE}/cases/{case_h6}/complete", token=sasuke_t)
        r = call("POST", f"{BASE}/cases/{case_h6}/close", token=sasuke_t)
        results.append(("TC-H06", "PASS" if r.status_code == 403 else "FAIL", f"HTTP {r.status_code} body={r.text[:200]}"))
    else:
        results.append(("TC-H06", "SKIP", "create failed"))

    write_results(results)
    # 印出總結
    p = sum(1 for x in results if x[1] == "PASS")
    f = sum(1 for x in results if x[1] == "FAIL")
    s = sum(1 for x in results if x[1] == "SKIP")
    print(f"\n===== SUMMARY: PASS={p} FAIL={f} SKIP={s} TOTAL={len(results)} =====")
    for tc, st, note in results:
        if st == "FAIL":
            print(f"  [{st}] {tc}: {note}")

def write_results(results):
    with open(r"D:\CaseFlow\CaseFlow\scripts\test_results.json", "w", encoding="utf-8") as f:
        json.dump([{"id": r[0], "status": r[1], "note": r[2]} for r in results],
                  f, ensure_ascii=False, indent=2)

if __name__ == "__main__":
    try:
        main()
    except Exception:
        traceback.print_exc()
        sys.exit(1)
