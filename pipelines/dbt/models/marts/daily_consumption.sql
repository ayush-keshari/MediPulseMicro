{{ config(materialized='table', alias='fct_daily_consumption') }}

select
    cast(ConsumedDate as date) as ConsumptionDate,
    FacilityId,
    ItemId,
    sum(cast(QuantityConsumed as bigint)) as TotalQuantityConsumed,
    count_big(*) as ConsumptionEventCount
from {{ ref('consumption_records') }}
group by cast(ConsumedDate as date), FacilityId, ItemId
