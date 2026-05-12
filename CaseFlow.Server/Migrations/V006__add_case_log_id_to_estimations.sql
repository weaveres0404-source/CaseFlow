-- V006: 在 case_estimations 加入 case_log_id，記錄評估是由哪一筆處理紀錄建立
ALTER TABLE case_estimations
    ADD COLUMN case_log_id INTEGER NULL
        REFERENCES case_logs(log_id) ON DELETE SET NULL;

CREATE INDEX idx_case_estimations_case_log_id ON case_estimations(case_log_id);
