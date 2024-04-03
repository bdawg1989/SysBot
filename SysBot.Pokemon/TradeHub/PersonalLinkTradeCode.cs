using System.Collections.Generic;

namespace PersonalCodeLogic
{
    public static class PersonalLinkTradeCode
    {
        // Define a dictionary to store user-specific Link Trade Codes
        private static readonly Dictionary<ulong, int> userLinkTradeCodes = new Dictionary<ulong, int>();

        // Method to set user's personal Link Trade Code
        public static void SetPersonalLinkTradeCode(ulong userId, int code)
        {
            userLinkTradeCodes[userId] = code;
        }

        // Method to get user's personal Link Trade Code
        public static int GetUserPersonalLinkTradeCode(ulong userId)
        {
            if (userLinkTradeCodes.TryGetValue(userId, out var code))
            {
                return code;
            }
            else
            {
                // Return 0 if the user hasn't set their personal Link Trade Code
                return 0;
            }
        }

        // Method to delete user's personal Link Trade Code
        public static void DeletePersonalLinkTradeCode(ulong userId)
        {
            if (userLinkTradeCodes.ContainsKey(userId))
            {
                userLinkTradeCodes.Remove(userId);
            }
        }
    }
}


