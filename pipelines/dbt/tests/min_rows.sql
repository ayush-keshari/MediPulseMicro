{% test min_rows(model, min_value) %}

    WITH validation AS (
        SELECT COUNT(*) AS row_count
        FROM {{ model }}
    )
    SELECT *
    FROM validation
    WHERE row_count < {{ min_value }}

{% endtest %}