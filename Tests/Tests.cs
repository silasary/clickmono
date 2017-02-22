using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ClickMac;

namespace Tests
{
    class Tests
    {
        [SetUp]
        public void Setup()
        {
            Loading.Log = Console.WriteLine;
        }

        [Test]
        public void DownloadPackager()
        {
            var packager = Loading.LoadWellKnownTool(Loading.KnownTools.Packager);
            Assert.AreEqual(0, packager.Options.Errors);
        }
    }
}
