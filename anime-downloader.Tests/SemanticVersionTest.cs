using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using anime_downloader.Models;
using NUnit.Framework;

namespace anime_downloader.Tests
{
    [TestFixture]
    public class SemanticVersionTest
    {
        private SemanticVersion _versionA;

        private SemanticVersion _versionB;

        [Test]
        public void VersionTest()
        {
            _versionA = new SemanticVersion("0.0.11");
            _versionB = new SemanticVersion("1.0.10");

            Assert.IsTrue(_versionA < _versionB);
            Assert.IsTrue(_versionB > _versionA);

            _versionA = new SemanticVersion("1.0.0");

            Assert.IsTrue(_versionA < _versionB);
            Assert.IsTrue(_versionB > _versionA);

            _versionA = new SemanticVersion("0.10.12");

            Assert.IsTrue(_versionA < _versionB);
            Assert.IsTrue(_versionB > _versionA);
        }
    }
}
