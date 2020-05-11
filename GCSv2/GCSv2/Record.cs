using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Web;

namespace GCSv2
{
    class Record
    {
        public void CreateCSVFile(DataTable dt, string strFilePath) // strFilePath 為輸出檔案路徑 (含檔名)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);

            int colCount = dt.Columns.Count;

            if (dt.Columns.Count > 0)
                sw.Write(dt.Columns[0]);
            for (int i = 1; i < dt.Columns.Count; i++)
                sw.Write("," + dt.Columns[i]);

            sw.Write(sw.NewLine);
            foreach (DataRow dr in dt.Rows)
            {
                if (dt.Columns.Count > 0 && !Convert.IsDBNull(dr[0]))
                    sw.Write(Encode(Convert.ToString(dr[0])));
                for (int i = 1; i < colCount; i++)
                    sw.Write("," + Encode(Convert.ToString(dr[i])));
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }

        public string Encode(string strEnc)
        {
            return HttpUtility.UrlEncode(strEnc);
        }
    }
}
