CREATE TABLE IF NOT EXISTS notes (
                                     id UUID PRIMARY KEY,
                                     user_id UUID NOT NULL,
                                     title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    is_trashed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
    );

CREATE INDEX IF NOT EXISTS ix_notes_user_id
    ON notes(user_id);

CREATE INDEX IF NOT EXISTS ix_notes_user_active
    ON notes(user_id, is_archived, is_trashed);

CREATE TABLE IF NOT EXISTS labels (
                                      id UUID PRIMARY KEY,
                                      user_id UUID NOT NULL,
                                      name VARCHAR(100) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_labels_user_name UNIQUE (user_id, name)
    );

CREATE TABLE IF NOT EXISTS note_labels (
                                           note_id UUID NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
    label_id UUID NOT NULL REFERENCES labels(id) ON DELETE CASCADE,
    PRIMARY KEY (note_id, label_id)
    );

CREATE INDEX IF NOT EXISTS ix_labels_user_id
    ON labels(user_id);