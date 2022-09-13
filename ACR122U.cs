using PCSC;
using PCSC.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace buluton.acr122u
{
    public class ACR122U
    {
        public delegate void CardInsertedHandler(ICardReader reader);

        public delegate void CardRemovedHandler();

        private int maxReadWriteLength = 50;

        private int blockSize = 4;

        private int startBlock = 4;

        private int readbackDelayMilliseconds = 100;

        private string[] cardReaderNames;

        private ISCardContext cardContext;

        private bool buzzerSet;

        private bool buzzerOnOff;

        public event CardInsertedHandler CardInserted;

        public event CardRemovedHandler CardRemoved;

        public void Init(bool buzzerOnOff, int maxReadWriteLength, int blockSize, int startBlock, int readbackDelayMilliseconds)
        {
            this.buzzerOnOff = buzzerOnOff;
            this.maxReadWriteLength = maxReadWriteLength;
            this.blockSize = blockSize;
            this.startBlock = startBlock;
            this.readbackDelayMilliseconds = readbackDelayMilliseconds;
            cardContext = ContextFactory.Instance.Establish(SCardScope.System);
            cardReaderNames = cardContext.GetReaders();
            ISCardMonitor ıSCardMonitor = MonitorFactory.Instance.Create(SCardScope.System);
            ıSCardMonitor.CardInserted += Monitor_CardInserted;
            ıSCardMonitor.CardRemoved += Monitor_CardRemoved;
            ıSCardMonitor.Start(cardReaderNames);
        }

        private void Monitor_CardInserted(object sender, CardStatusEventArgs e)
        {
            ICardReader cardReader = null;
            try
            {
                cardReader = cardContext.ConnectReader(cardReaderNames[0], SCardShareMode.Shared, SCardProtocol.Any);
            }
            catch
            {
            }

            if (cardReader != null)
            {
                if (!buzzerSet)
                {
                    buzzerSet = true;
                    SetBuzzer(cardReader, buzzerOnOff);
                }

                this.CardInserted?.Invoke(cardReader);
                try
                {
                    cardReader.Disconnect(SCardReaderDisposition.Leave);
                }
                catch
                {
                }
            }
        }

        private void Monitor_CardRemoved(object sender, CardStatusEventArgs e)
        {
            this.CardRemoved?.Invoke();
        }

        public byte[] GetUID(ICardReader reader)
        {
            byte[] array = new byte[10];
            reader.Transmit(new byte[5] { 255, 202, 0, 0, 0 }, array);
            Array.Resize(ref array, 7);
            return array;
        }

        public byte[] Read(ICardReader reader, int block, int len)
        {
            byte[] array = new byte[len + 2];
            reader.Transmit(new byte[5]
            {
                255,
                176,
                0,
                (byte)block,
                (byte)len
            }, array);
            Array.Resize(ref array, len);
            return array;
        }

        public void Write(ICardReader reader, int block, int len, byte[] data)
        {
            byte[] receiveBuffer = new byte[2];
            List<byte> list = new byte[5]
            {
                255,
                214,
                0,
                (byte)block,
                (byte)len
            }.ToList();
            list.AddRange(data);
            reader.Transmit(list.ToArray(), receiveBuffer);
        }

        public bool WriteData(ICardReader reader, byte[] data)
        {
            Array.Resize(ref data, maxReadWriteLength);
            for (int i = 0; i < data.Length; i += blockSize)
            {
                byte[] array = new byte[blockSize];
                int length = ((data.Length - i > blockSize) ? blockSize : (data.Length - i));
                Array.Copy(data, i, array, 0, length);
                Write(reader, i / blockSize + startBlock, blockSize, array);
            }

            Thread.Sleep(readbackDelayMilliseconds);
            byte[] second = ReadData(reader);
            return data.SequenceEqual(second);
        }

        public byte[] ReadData(ICardReader reader)
        {
            List<byte> list = new List<byte>();
            for (int i = 0; i < maxReadWriteLength; i += blockSize)
            {
                int len = ((maxReadWriteLength - i > blockSize) ? blockSize : (maxReadWriteLength - i));
                byte[] collection = Read(reader, i / blockSize + startBlock, len);
                list.AddRange(collection);
            }

            return list.ToArray();
        }

        public void SetBuzzer(ICardReader reader, bool on)
        {
            byte[] receiveBuffer = new byte[2];
            reader.Transmit(new byte[5]
            {
                255,
                0,
                82,
                (byte)(on ? 255u : 0u),
                0
            }, receiveBuffer);
        }
    }
}