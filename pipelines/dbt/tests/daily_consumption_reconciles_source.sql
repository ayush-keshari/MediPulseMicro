-- Compare every aggregate in both directions, not only the grand total.
select * from (
    select cast(ConsumedDate as date) as ConsumptionDate, FacilityId, ItemId,
           sum(cast(QuantityConsumed as bigint)) as TotalQuantityConsumed,
           count_big(*) as ConsumptionEventCount
    from {{ source('mediapulse_main', 'ConsumptionRecord') }}
    group by cast(ConsumedDate as date), FacilityId, ItemId
    except
    select ConsumptionDate, FacilityId, ItemId, TotalQuantityConsumed, ConsumptionEventCount
    from {{ ref('daily_consumption') }}
) as missing_or_changed
union all
select * from (
    select ConsumptionDate, FacilityId, ItemId, TotalQuantityConsumed, ConsumptionEventCount
    from {{ ref('daily_consumption') }}
    except
    select cast(ConsumedDate as date), FacilityId, ItemId,
           sum(cast(QuantityConsumed as bigint)), count_big(*)
    from {{ source('mediapulse_main', 'ConsumptionRecord') }}
    group by cast(ConsumedDate as date), FacilityId, ItemId
) as unexpected_or_changed
