{{ config(materialized='view', alias='stg_consumption_records') }}

select
    ConsumptionId,
    FacilityId,
    ItemId,
    QuantityConsumed,
    ConsumedDate
from {{ source('mediapulse_main', 'ConsumptionRecord') }}
