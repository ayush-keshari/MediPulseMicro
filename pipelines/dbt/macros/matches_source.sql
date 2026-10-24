{% test matches_source(model, source_relation, columns) %}

select * from (
    select {{ columns | join(', ') }} from {{ source_relation }}
    except
    select {{ columns | join(', ') }} from {{ model }}
) as missing_rows
union all
select * from (
    select {{ columns | join(', ') }} from {{ model }}
    except
    select {{ columns | join(', ') }} from {{ source_relation }}
) as unexpected_rows

{% endtest %}
