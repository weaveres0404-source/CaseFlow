-- Migration: V004 — 新增 case_type_filter 欄位，並依附圖重置問題分類資料
-- 執行方式：連線至 PostgreSQL 後執行此 SQL（或透過 psql \i 命令）

-- 1. 在 problem_categories 加入 case_type_filter 欄位（對應案件類型 enum value）
ALTER TABLE problem_categories
  ADD COLUMN IF NOT EXISTS case_type_filter VARCHAR(20) NULL;

COMMENT ON COLUMN problem_categories.case_type_filter IS '對應案件類型（REPAIR/EVALUATION/MAINTENANCE/UHD），NULL 表示所有類型皆可選';

-- 2. 清除舊分類，改用附圖定義的標準分類
TRUNCATE TABLE problem_categories RESTART IDENTITY CASCADE;

-- 3. 插入標準分類（依案件類型分組）

-- 障礙調查 (REPAIR)
INSERT INTO problem_categories (category_name, case_type_filter, sort_order, is_active, created_at, updated_at) VALUES
  ('版本上線異常',   'REPAIR', 1,  true, NOW(), NOW()),
  ('日常操作異常',   'REPAIR', 2,  true, NOW(), NOW()),
  ('三代介接異常',   'REPAIR', 3,  true, NOW(), NOW()),
  ('DB連線異常',     'REPAIR', 4,  true, NOW(), NOW()),
  ('其他',           'REPAIR', 99, true, NOW(), NOW());

-- 工時評估 (EVALUATION)
INSERT INTO problem_categories (category_name, case_type_filter, sort_order, is_active, created_at, updated_at) VALUES
  ('新需求開發評估', 'EVALUATION', 1,  true, NOW(), NOW()),
  ('系統改善評估',   'EVALUATION', 2,  true, NOW(), NOW()),
  ('資料異動評估',   'EVALUATION', 3,  true, NOW(), NOW()),
  ('配合測試評估',   'EVALUATION', 4,  true, NOW(), NOW()),
  ('介接調整評估',   'EVALUATION', 5,  true, NOW(), NOW()),
  ('其他',           'EVALUATION', 99, true, NOW(), NOW());

-- 日常維運 (MAINTENANCE)
INSERT INTO problem_categories (category_name, case_type_filter, sort_order, is_active, created_at, updated_at) VALUES
  ('人員密碼重置',   'MAINTENANCE', 1,  true, NOW(), NOW()),
  ('PP派工通知調查', 'MAINTENANCE', 2,  true, NOW(), NOW()),
  ('歷史資料清除',   'MAINTENANCE', 3,  true, NOW(), NOW()),
  ('配合系統測試',   'MAINTENANCE', 4,  true, NOW(), NOW()),
  ('資料調查',       'MAINTENANCE', 5,  true, NOW(), NOW()),
  ('定期資料檢查',   'MAINTENANCE', 6,  true, NOW(), NOW()),
  ('其他',           'MAINTENANCE', 99, true, NOW(), NOW());

-- UHD協助 (UHD)
INSERT INTO problem_categories (category_name, case_type_filter, sort_order, is_active, created_at, updated_at) VALUES
  ('兩岸退貨退款處理', 'UHD', 1,  true, NOW(), NOW()),
  ('消費者資料刪除',   'UHD', 2,  true, NOW(), NOW()),
  ('撈取資料',         'UHD', 3,  true, NOW(), NOW()),
  ('資料異動',         'UHD', 4,  true, NOW(), NOW()),
  ('單位資料撈取',     'UHD', 5,  true, NOW(), NOW()),
  ('系統參數設定',     'UHD', 6,  true, NOW(), NOW()),
  ('後台DB資料修改',   'UHD', 7,  true, NOW(), NOW()),
  ('其他',             'UHD', 99, true, NOW(), NOW());
