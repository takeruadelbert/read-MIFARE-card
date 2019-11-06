using System;
using System.Collections.Generic;
using System.Text;

namespace MiFareCard.ConstantVariable
{
    class Constant
    {
        public static readonly string READER_NAME = "ACS ACR123U-A1";
        public static readonly string CARD_INSERTED = "Card is inserted.";
        public static readonly string CARD_REMOVED = "Card is removed.";
        public static readonly string BREAKLINE = "\n";

        public static readonly string ERROR_FAIL_TO_READ_LIST_READER = "Fail to read list reader.";
        public static readonly string ERROR_FAIL_TO_READ_UID_CARD = "Cannot Read UID Card.";
        public static readonly string ERROR_FAIL_TO_ESTABLISH_CONTEXT = "Check your device and please restart again.";
    }
}
