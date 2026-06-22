-- 將 project_codes.code 改為 2 碼短代號（編號），label 保留原專案中文名稱
-- 對應關係：BK-ATM→BK, FB-POS→FB, LG-DISP→LG, SM-MAINT→SM, TC-CRM→TC, OE→OE
-- projects.project_code（專案代號）仍維持原長碼

BEGIN;

-- 1. 更新資料（以舊長碼為條件，避免依賴 code_id）
UPDATE project_codes SET code = 'BK', updated_at = now() WHERE code = 'BK-ATM';
UPDATE project_codes SET code = 'FB', updated_at = now() WHERE code = 'FB-POS';
UPDATE project_codes SET code = 'LG', updated_at = now() WHERE code = 'LG-DISP';
UPDATE project_codes SET code = 'SM', updated_at = now() WHERE code = 'SM-MAINT';
UPDATE project_codes SET code = 'TC', updated_at = now() WHERE code = 'TC-CRM';
-- OE 已是 2 碼，不需更新

-- 2. 收緊欄位長度為 VARCHAR(10) 以反映「短編號」的結構意圖
ALTER TABLE project_codes ALTER COLUMN code TYPE VARCHAR(10);

-- 3. 驗證
SELECT code_id, code, label FROM project_codes ORDER BY code_id;

COMMIT;
