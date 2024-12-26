using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FAT.MetaData
{
    public class BootCode
    {
        public string magicNumber {  get; set; }

        public BootCode()
        {
            magicNumber = "";
        }

        [JsonConstructor]
        public BootCode(string magicNumber)
        {
            this.magicNumber = magicNumber;
        }

        public void recalculateMagicNumber(Fat fat)
        {
            string jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = false, IgnoreReadOnlyProperties = true });
            magicNumber = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(jsonString)));
        }

        public bool boot(Fat fat)
        {
            string jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = false, IgnoreReadOnlyProperties = true });
            string mN = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(jsonString)));
            return magicNumber == mN;
        }

        public override string ToString()
        {
            return $"Magic Number: {magicNumber}";
        }
    }
}
