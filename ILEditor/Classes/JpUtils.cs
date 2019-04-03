using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILEditor.Classes
{
    /*
    public enum DownLoadTypes
    {
        MBR, MBRL
    }
    */

    class JpUtils
    {
        /*
        private static int _DownLoadType;

        public static void SetDownLoadTypes(int DownLoadType)
        {
            JpUtils._DownLoadType = DownLoadType;
        }

        public static int GetDownLoadTypes()
        {
            return JpUtils._DownLoadType;
        }
        */
        // IFSの日本語変換用テンポラリディレクトリ取得
        public static string GetRootTmpDir()
        {
            return "/TMP/ILEDITOR";
        }
        // IFSの日本語変換用テンポラリディレクトリ取得
        public static string GetTmpDir()
        {
            return GetRootTmpDir() + "/" + IBMi.CurrentSystem.GetValue("username").ToUpper();
        }

        // IFSの日本語変換用ダウンロードテンポラリファイル名取得(メンバー用)
        public static string GetDwTmpFileNameMbr()
        {
            return GetTmpDir() + "/DOWNMBR.TMP";
        }

        // IFSの日本語変換用ダウンロードファイル名取得(メンバー用)
        public static string GetDwFileNameMbr()
        {
            return GetTmpDir() + "/DOWNMBR";
        }

        // IFSの日本語変換用ダウンロードテンポラリファイル名取得(ＩＦＳ用)
        public static string GetDwTmpFileNameIfs()
        {
            return GetTmpDir() + "/DOWNIFS.TMP";
        }

        // IFSの日本語変換用ダウンロードファイル名取得(ＩＦＳ用)
        public static string GetDwFileNameIfs()
        {
            return GetTmpDir() + "/DOWNIFS";
        }

        // IFSの日本語変換用ダウンロードテンポラリファイル名取得(メンバー／ＩＦＳ以外)
        public static string GetDwTmpFileName()
        {
            return GetTmpDir() + "/DOWNLOAD.TMP";
        }

        // IFSの日本語変換用ダウンロードファイル名取得(メンバー／ＩＦＳ以外)
        public static string GetDwFileName()
        {
            return GetTmpDir() + "/DOWNLOAD";
        }

        // IFSの日本語変換用アップロードテンポラリファイル名取得
        public static string GetUpTmpFileName()
        {
            return GetTmpDir() + "/UPLOAD.TMP";
        }

        // IFSの日本語変換用アップロードファイル名取得
        public static string GetUpFileName()
        {
            return GetTmpDir() + "/UPLOAD";
        }

        // QTEMPに作成するテンポラリファイルのレコード長
        public static string GetQtempRcdLen()
        {
            return "198";
        }

        // /QSYS.lib/XXXLIB.lib/XXXSRC.file/XXXMBR.mbr形式をもらい、分割して返却
        public static string[] GetRemoteName(String str)
        {
            var retArr = new String[3];
            String tmp = str.Substring(10); // /QSYS.lib/を除外
            string[] arr = tmp.Split('/');  // LIB.FILE,MBRに分割
            retArr[0] = arr[0].Replace(".lib", "");
            retArr[1] = arr[1].Replace(".file", "");
            retArr[2] = arr[2].Replace(".mbr", "");
            return retArr;
        }
    }
}
