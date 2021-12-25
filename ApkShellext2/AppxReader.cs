﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace ApkShellext2
{
    /// <summary>
    /// 
    /// </summary>
    public class AppxReader : AppPackageReader
    {
        private const string AppxManifestXml = @"AppxManifest.xml";
        //private const string elemPackage = @"Package";
        private const string elemIdentity = @"Identity";
        private const string elemProperties = @"Properties";
        private const string elemDisplayName = @"DisplayName";
        private const string elemLogo = @"Logo";
        private const string elemPhoneIdentity = @"mp:PhoneIdentity";

        private const string attrVersion = @"Version";
        private const string attrName = @"Name";
        private const string attrPublisher = @"Publisher";
        private const string attrPhoneProductID = @"PhoneProductId";

        private ZipFile zip;
        private string iconPath;

        private string version;
        private string appname;
        private string packageName;
        private string publisher;
        private string productid;

        public AppxReader(Stream stream)
        {
            FileName = "";
            zip = new ZipFile(stream);
            Extract();
        }

        public AppxReader(string path)
        {
            FileName = path;
            zip = new ZipFile(FileName);
            Extract();
        }

        public void Extract()
        {
            ZipEntry en = zip.GetEntry(AppxManifestXml);
            if (en == null)
                throw new EntryPointNotFoundException("cannot find " + AppxManifestXml);
            byte[] xmlbytes = new byte[en.Size];
            zip.GetInputStream(en).Read(xmlbytes, 0, (int)en.Size);

            XmlDocument xml = new XmlDocument();
            xml.XmlResolver = null;
            xml.Load(zip.GetInputStream(en));

            XmlElement packageNode = xml.DocumentElement;
            XmlElement Identity = packageNode[elemIdentity];
            version = Identity.Attributes[attrVersion].Value.ToString();
            packageName = Identity.Attributes[attrName].Value.ToString();
            publisher = Identity.Attributes[attrPublisher].Value.ToString();
            Match m = Regex.Match(publisher, @"CN=([^,]*),?");
            if (m.Success)
            {
                publisher = m.Groups[1].Value;
            }

            XmlElement DisplayName = packageNode[elemProperties][elemDisplayName];
            appname = DisplayName.FirstChild.Value.ToString();
            XmlElement Logo = packageNode[elemProperties][elemLogo];
            iconPath = Logo.FirstChild.Value.ToString().Replace(@"\", @"/");
            XmlElement PhoneIdentity = packageNode[elemPhoneIdentity];
            productid = PhoneIdentity.Attributes[attrPhoneProductID].Value.ToString();
        }

        public override AppPackageReader.AppType Type
        {
            get
            {
                return AppType.WindowsPhoneApp;
            }
        }

        public override string AppName
        {
            get
            {
                return appname;
            }
        }

        public override string Version
        {
            get
            {
                return version;
            }
        }

        public override string Publisher
        {
            get
            {
                return publisher;
            }
        }

        public override string PackageName
        {
            get
            {
                return packageName;
            }
        }

        public override string AppID
        {
            get
            {
                return productid;
            }
        }

        public override Bitmap Icon
        {
            get
            {
                if (iconPath == "")
                    throw new Exception("Cannot find logo path");
                int dot = iconPath.LastIndexOf(".");
                string name = iconPath.Substring(0, dot);
                string extension = iconPath.Substring(dot + 1);
                ZipEntry logo = null;
                int scale = -1;
                foreach (ZipEntry en in zip)
                {
                    Match m = Regex.Match(en.Name, name + @"(\.scale\-(\d+))?" + @"\." + extension);
                    if (m.Success)
                    {
                        if (m.Groups[1].Value == "")
                        { // exactly matching, no scale
                            logo = en;
                            break;
                        }
                        else
                        { // find the biggest scale
                            int newScale = int.Parse(m.Groups[2].Value); if (newScale > scale)
                            {
                                logo = en;
                                scale = newScale;
                            }
                        }
                    }
                }
                if (logo != null)
                {
                    //byte[] imageBytes = new byte[logo.Size];
                    //zip.GetInputStream(logo).Read(imageBytes, 0, (int)logo.Size);
                    return new Bitmap(zip.GetInputStream(logo));
                }
                else
                {
                    throw new EntryPointNotFoundException("Cannot find Logo file: " + iconPath);
                }
            }
        }

        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                if (zip != null)
                    zip.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void Close()
        {
            Dispose(true);
        }

        ~AppxReader()
        {
            Dispose(true);
        }

    }
}
