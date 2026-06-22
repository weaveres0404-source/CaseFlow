-- 修正分類名稱
-- Fix 1: PPC/工通知調查 → PP派工通知調查
UPDATE problem_categories
SET category_name = 'PP派工通知調查',
    updated_at    = NOW()
WHERE category_name = 'PPC/工通知調查';

-- Fix 2: 總部及應問題調查 → 總部應問題調查
UPDATE problem_categories
SET category_name = '總部應問題調查',
    updated_at    = NOW()
WHERE category_name = '總部及應問題調查';

-- 確認結果
SELECT category_id, category_name, case_type_filter, sort_order
FROM   problem_categories
WHERE  case_type_filter = 'MAINTENANCE'
ORDER  BY sort_order;
