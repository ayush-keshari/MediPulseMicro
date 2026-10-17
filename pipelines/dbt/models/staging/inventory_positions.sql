{{ config(materialized='view', alias='stg_inventory_positions') }}

select
    PositionId,
    ItemId,
    LotId,
    Quantity,
    FacilityId,
    StorageZoneId,
    IngestedAt
from {{ source('mediapulse_main', 'InventoryPositions') }}
