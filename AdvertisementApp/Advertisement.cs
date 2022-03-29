//using System;
//using System.Collections.Generic;
//using System.Data.SQLite;
//using System.Text;

//namespace AdvertisementApp
//{
//    class Advertisement
//    {
//        //private Advertiser advertiser;

//        public ulong ID { get; set; }

//        public ulong AdvertiserID { get; set; }

//        public string Link { get; set; }

//        public DateTime ExpiryDate { get; set; }

//        public DateTime RegistrationDate { get; set; }

//        public bool IsExpired => DateTime.UtcNow > ExpiryDate;

//        //public Advertiser Advertiser
//        //{
//        //    get
//        //    {
//        //        if (advertiser == null)
//        //        {
//        //            SQLiteCommand getAdvertiser = new SQLiteCommand($"SELECT * FROM advertisers WHERE id = {AdvertiserID}");
//        //            var reader = getAdvertiser.ExecuteReader();
//        //            if (reader.Read())
//        //            {
//        //                advertiser = Advertiser.Parse(reader);
//        //            }
//        //        }
//        //        return advertiser;
//        //    }
//        //    private set => advertiser = value;
//        //}

//        public Advertisement()
//        {

//        }

//        public Advertisement(ulong id, ulong advertiser, string link, DateTime expDate, DateTime regDate)
//        {
//            ID = id;
//            AdvertiserID = advertiser;
//            Link = link;
//            ExpiryDate = expDate;
//            RegistrationDate = regDate;
//        }

//        //public Advertisement(ulong id, Advertiser advertiser, string link, DateTime expDate, DateTime regDate) : this(id, advertiser.ID, link, expDate, regDate)
//        //{
//        //    Advertiser = advertiser;
//        //}

//        //public static Advertisement Parse(SQLiteDataReader reader)
//        //{

//        //}

//        //public static List<Advertisement> MassParse(SQLiteDataReader reader)
//        //{

//        //}
//    }
//}
