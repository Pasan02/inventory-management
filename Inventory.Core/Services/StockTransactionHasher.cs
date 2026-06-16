using inventory_management.Data.Entities;
using System;
using System.Security.Cryptography;
using System.Text;

namespace inventory_management.Services
{
    public static class StockTransactionHasher
    {
        public static string ComputeChecksum(StockTransaction transaction)
        {
            var payload = $"{transaction.ItemId}|{transaction.ActionType}|{transaction.QuantityChange}|{transaction.Timestamp:O}|{transaction.MachineName}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }
    }
}
