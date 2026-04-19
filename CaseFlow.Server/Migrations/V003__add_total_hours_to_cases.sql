-- Migration: V003 — 在 cases 資料表新增 total_hours 欄位
-- 執行方式：連線至 PostgreSQL 後執行此 SQL（或透過 psql \i 命令）
-- 說明：記錄每筆案件的累計投入工時，由後端 API 隨每次 CaseLog 新增時自動更新

ALTER TABLE cases
  ADD COLUMN IF NOT EXISTS total_hours NUMERIC(7, 2) NOT NULL DEFAULT 0;

-- 回填歷史資料：依 case_logs 計算各案件目前的累計工時
UPDATE cases c
SET total_hours = COALESCE((
  SELECT SUM(cl.hours_spent)
  FROM case_logs cl
  WHERE cl.case_id = c.case_id
), 0);

COMMENT ON COLUMN cases.total_hours IS '累計投入工時（小時），由 API 於每次新增/修改 CaseLog 時自動更新';
