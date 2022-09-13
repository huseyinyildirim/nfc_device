# NDEF Library for NFC

https://github.com/huseyinyildirim/nfc_device

https://github.com/huseyinyildirim/ndef

```csharp
namespace nfc_ndef
{
    public class Proccess
    {
        private static readonly ACR122U _acr122u = new ACR122U();
        private static string _webServiceUrl;

        public static async Task NfcListAsync()
        {
            try
            {
                _acr122u.Init(false, 100, 4, 4, 200);  // NTAG213
                _acr122u.CardInserted += Acr122u_CardInserted;
                _acr122u.CardRemoved += Acr122u_CardRemoved;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Nfc List Async");
            }
        }

        private static void Acr122u_CardInserted(PCSC.ICardReader reader)
        {
            if (reader.IsConnected)
            {
                NdefMessage message = new NdefMessage();

                Payload payload = new UriPayload(Global.UriPrefixStringForUrl(_webServiceUrl), Global.UriPrefixClearForUrl(_webServiceUrl));
                message.AddRecord(new NdefRecord(payload));

                Log.Information("NFC kart okundu. Kart ID: " + BitConverter.ToString(_acr122u.GetUID(reader)).Replace("-", ""));
                bool status = _acr122u.WriteData(reader, message.ToBinaryData());
                Log.Information("Yazma İşlemi: " + (status ? "Başarılı" : "Başarısız"));
            }
            else
            {
                Log.Information("NFC cihazı bağlı değildir!");
            }
        }

        private static void Acr122u_CardRemoved()
        {
            Log.Information("NFC kart kaldırıldı.");
        }
    }
}
```
