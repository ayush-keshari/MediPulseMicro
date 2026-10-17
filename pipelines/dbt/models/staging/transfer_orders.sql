{{ config(materialized='view', alias='stg_transfer_orders') }}

select
    TransferOrderId,
    FromFacilityId,
    FromFacilityName,
    ToFacilityId,
    ToFacilityName,
    RequestedBy,
    RequestedDate,
    Status
from {{ source('mediapulse_main', 'TransferOrder') }}
