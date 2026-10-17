{{ config(materialized='view', alias='stg_receipts') }}

select
    ReceiptID,
    POID,
    SupplierLot,
    ReceivedDate,
    ReceivedBy,
    QualityStatus,
    QuantityReceived
from {{ source('mediapulse_main', 'Receipt') }}
