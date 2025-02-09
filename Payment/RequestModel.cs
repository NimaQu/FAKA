﻿using FAKA.Server.Models;

namespace FAKA.Server.Payment;

public class PaymentRequest
{
    public Order Order { get; set; }
    public Gateway Gateway { get; set; }
    public string? ReturnUrl { get; set; }
    public string TradeNumber { get; set; }
}