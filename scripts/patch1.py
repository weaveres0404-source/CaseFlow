"""補丁工具：把 assignee_user_ids 改成 se_user_ids"""
import re, pathlib
p = pathlib.Path(r"D:\CaseFlow\CaseFlow\scripts\run_api_tests.py")
s = p.read_text(encoding="utf-8")
s = s.replace('"assignee_user_ids"', '"se_user_ids"')
# 加入 primary_se_user_id（第一個就是 primary）
s = s.replace('{"se_user_ids": [nami_uid], "instruction": "請優先釐清"}',
              '{"se_user_ids": [nami_uid], "primary_se_user_id": nami_uid, "instructions": "請優先釐清"}')
s = re.sub(r'\{"se_user_ids": \[(\w+_uid)\]\}', r'{"se_user_ids": [\1], "primary_se_user_id": \1}', s)
s = s.replace('{"se_user_ids": [nami_uid, sasuke_uid]}',
              '{"se_user_ids": [nami_uid, sasuke_uid], "primary_se_user_id": nami_uid}')
p.write_text(s, encoding="utf-8")
print("patched")
