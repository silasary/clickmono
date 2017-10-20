using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ClickMac;
using System.IO;

namespace Tests
{
    class Tests
    {
        public static string TestLibrary
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(typeof(Tests).Assembly.Location), "Library");
            }
        }

        [SetUp]
        public void Setup()
        {
            Loading.Log = Console.WriteLine;
            if (Directory.Exists(TestLibrary))
                Directory.Delete(TestLibrary, true);
            Directory.CreateDirectory(TestLibrary);
            Platform.LibraryLocation = TestLibrary;
        }

        [Test]
        public void DownloadPackager()
        {
            var packager = Loading.LoadWellKnownTool(Loading.KnownTools.Packager);
            Assert.AreEqual(0, packager.Options.Errors);
        }
    }
}
