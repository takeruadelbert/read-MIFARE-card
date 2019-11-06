using PCSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MiFareCard.ConstantVariable;

namespace MiFareCard
{
    class Main
    {
        private int retCode;
        private IntPtr hCard;
        private IntPtr hContext;
        private IntPtr Protocol;
        private bool connActive = false;
        private string readername = Constant.READER_NAME;

        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public Card.SCARD_READERSTATE RdrState;

        SynchronizationContext context;
        private static readonly IContextFactory contextFactory = ContextFactory.Instance;
        SCardMonitor sCardMonitor = new SCardMonitor(contextFactory, SCardScope.System);

        public Main()
        {
            establishContext();
            Console.WriteLine("===========");
            Console.WriteLine("List Reader");
            Console.WriteLine("===========");
        }

        public string SelectDevice()
        {            
            List<string> availableReaders = this.ListReaders();
            this.RdrState = new Card.SCARD_READERSTATE();
            readername = availableReaders[0].ToString(); //selecting first device
            this.RdrState.RdrName = readername;
            return readername;
        }

        public List<string> ListReaders()
        {
            UInt32 ReaderCount = 0;
            List<string> AvailableReaderList = new List<string>();

            // Make sure a context has been established before retrieving the list of smartcard readers.
            retCode = Card.SCardListReaders(hContext, null, null, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                connActive = false;
                Console.WriteLine(Constant.ERROR_FAIL_TO_READ_LIST_READER);

            }

            byte[] ReadersList = new byte[ReaderCount];

            // Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
            retCode = Card.SCardListReaders(hContext, null, ReadersList, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.WriteLine(Constant.ERROR_FAIL_TO_READ_LIST_READER);
            }

            string rName = "";
            int indx = 0, counter = 1;
            if (ReaderCount > 0)
            {
                // Convert reader buffer to string
                while (ReadersList[indx] != 0)
                {

                    while (ReadersList[indx] != 0)
                    {
                        rName += (char)ReadersList[indx];
                        indx++;
                    }

                    // Add reader name to list                    
                    AvailableReaderList.Add(rName);                    
                    indx++;
                    Console.WriteLine(counter + ". " + rName);
                    rName = "";
                    counter++;

                }
            }
            return AvailableReaderList;

        }
        public bool connectCard()
        {
            connActive = true;

            retCode = Card.SCardConnect(hContext, readername, Card.SCARD_SHARE_SHARED, Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                connActive = false;
                return false;
            }
            return true;
        }

        private string getcardUID()//only for mifare 1k cards
        {
            string cardUID = "";
            byte[] receivedUID = new byte[256];

            Card.SCARD_IO_REQUEST request = new Card.SCARD_IO_REQUEST();
            request.dwProtocol = Card.SCARD_PROTOCOL_T1;
            request.cbPciLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Card.SCARD_IO_REQUEST));

            byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }; //get UID command for Mifare cards
            uint outBytes = (uint)receivedUID.Length;
            int status = Card.SCardTransmit(hCard, ref request, ref sendBytes[0], (uint)sendBytes.Length, ref request, receivedUID, ref outBytes);

            if (status != Card.SCARD_S_SUCCESS)
            {
                cardUID = Constant.ERROR_FAIL_TO_READ_UID_CARD;
            }
            else
            {
                cardUID = BitConverter.ToString(receivedUID.Take(4).ToArray());
                cardUID = string.Join("-", cardUID.Split("-").Reverse());
                cardUID = cardUID.Replace("-", string.Empty).ToLower();
                cardUID = UInt32.Parse(cardUID, System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(10, '0');
            }

            return cardUID;
        }

        internal void establishContext()
        {
            retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_SYSTEM, IntPtr.Zero, IntPtr.Zero, out hContext);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.WriteLine(Constant.ERROR_FAIL_TO_ESTABLISH_CONTEXT);
                connActive = false;
                return;
            }
        }

        public void disconnect()
        {
            Card.SCardDisconnect(hCard, 0);
            Card.SCardReleaseContext(hCard);
        }

        public void connect()
        {
            if (connectCard())
            {
                string cardUID = getcardUID();
                Console.WriteLine("UID = " + cardUID);
            }
        }

        private void card_CardInserted(object sender, EventArgs e)
        {
            context.Post(new SendOrPostCallback(o =>
            {
                Console.WriteLine(Constant.BREAKLINE + Constant.CARD_INSERTED);
                connect();
            }), null);
        }

        private void card_CardRemoved(object sender, EventArgs e)
        {
            context.Post(new SendOrPostCallback(o =>
            {
                Console.WriteLine(Constant.BREAKLINE + Constant.CARD_REMOVED);
                disconnect();
            }), null);
        }

        public void RunMain()
        {
            context = SynchronizationContext.Current;
            if (context == null)
            {
                context = new SynchronizationContext();
            }

            sCardMonitor.CardInserted += new CardInsertedEvent(card_CardInserted);
            sCardMonitor.CardRemoved += new CardRemovedEvent(card_CardRemoved);
            sCardMonitor.Start(SelectDevice());
        }
    }
}