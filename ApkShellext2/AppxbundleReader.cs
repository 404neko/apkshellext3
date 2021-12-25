﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Drawing;
using System.IO;
using System.Xml;

namespace ApkShellext2
{
    /// <summary>
    /// Read AppxBundle
    /// </summary>
    public class AppxBundleReader : AppPackageReader
    {
        private ZipFile zip;
        private AppxReader appxReader = null;

        private const string AppxBundleManifestXml = @"AppxMetadata/AppxBundleManifest.xml";
        private const string ElemIdentity = @"Identity";
        //private const string ElemProperties = @"Properties";
        //private const string ElemDisplayName = @"DisplayName";
        private const string ElemPackage = @"Package";
        //private const string AttrVersion = @"Version";
        private const string AttrName = @"Name";
        //private const string AttrPublisher = @"Publisher";
        private const string AttrType = @"Type";
        private const string ValApplication = @"application";
        private const string AttrFileName = @"FileName";

        public AppxBundleReader(Stream stream)
        {
            FileName = "";
            openFile(stream);
        }

        public AppxBundleReader(string path)
        {
            FileName = path;
            openFile(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void openFile(Stream stream)
        {
            string appxFileName = "";
            zip = new ZipFile(stream);
            ZipEntry en = zip.GetEntry(AppxBundleManifestXml);
            if (en == null)
                throw new EntryPointNotFoundException("cannot find " + AppxBundleManifestXml);

            using (XmlReader reader = XmlReader.Create(zip.GetInputStream(en)))
            {
                reader.ReadToFollowing(ElemIdentity);
                reader.MoveToAttribute(AttrName);

                do
                {
                    reader.ReadToFollowing(ElemPackage);
                    reader.MoveToAttribute(AttrType);
                } while (reader.Value != ValApplication || reader.EOF);

                if (reader.EOF)
                    throw new Exception("Cannot find application in " + AppxBundleManifestXml);

                reader.MoveToAttribute(AttrFileName);
                appxFileName = reader.Value;
            }

            en = zip.GetEntry(appxFileName);
            if (en == null)
                throw new EntryPointNotFoundException("cannot find appx " + appxFileName);

            appxReader = new AppxReader(zip.GetInputStream(en));
        }

        public override AppPackageReader.AppType Type
        {
            get
            {
                return AppType.WindowsPhoneAppBundle;
            }
        }

        public override Bitmap Icon
        {
            get
            {
                return appxReader.Icon;
            }
        }

        public override string AppName
        {
            get
            {
                return appxReader.AppName;
            }
        }

        public override string PackageName
        {
            get
            {
                return appxReader.PackageName;
            }
        }

        public override string Version
        {
            get
            {
                return appxReader.Version;
            }
        }

        public override string Publisher
        {
            get
            {
                return appxReader.Publisher;
            }
        }

        public override string AppID
        {
            get
            {
                return appxReader.AppID;
            }
        }
        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                if (appxReader != null)
                {
                    appxReader.Close();
                }
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

        ~AppxBundleReader()
        {
            Dispose(true);
        }
    }
}
