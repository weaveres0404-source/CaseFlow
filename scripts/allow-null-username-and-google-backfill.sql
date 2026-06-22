-- 允許 users.username / users.full_name 為 NULL，
-- 並改成只對「非空 username」做唯一檢查
-- 建議先在 test 跑完驗證，再套到 prod

BEGIN;

-- 1) 先把空字串清成 NULL，避免後續唯一鍵與語意混亂
UPDATE public.users
SET username = NULL,
    updated_at = NOW()
WHERE username IS NOT NULL
  AND btrim(username) = '';

-- 2) username / full_name 欄位允許 NULL
ALTER TABLE public.users
ALTER COLUMN username DROP NOT NULL;

ALTER TABLE public.users
ALTER COLUMN full_name DROP NOT NULL;

-- 3) 移除舊的唯一限制 / 索引（名稱以你目前環境常見的為主，存在才刪）
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS uk_users_username;
DROP INDEX IF EXISTS public.uk_users_username;
DROP INDEX IF EXISTS public.idx_users_username;

-- 4) 建立新的部分唯一索引：只有非空 username 才必須唯一
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username_not_blank
ON public.users (username)
WHERE username IS NOT NULL AND btrim(username) <> '';

COMMIT;
