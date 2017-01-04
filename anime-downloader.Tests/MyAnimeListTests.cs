using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using anime_downloader.Tests.Services;
using NUnit.Framework;

namespace anime_downloader.Tests
{
    [TestFixture]
    public class MyAnimeListTests
    {
        private ISettingsService _settings;
        private IAnimeService _animes;
        private IMyAnimeListService _mal;

        [SetUp]
        public void Init()
        {
            _settings = new MockXmlSettingsService
            {
                MyAnimeListConfig =
                    new MyAnimeListConfiguration
                    {
                        Username = Credentials.MyAnimeListName,
                        Password = Credentials.MyAnimeListPassword
                    }
            };
            _animes = new MockAnimeService();
            _mal = new MyAnimeListService(_settings, _animes);
        }

        private async Task TestId(string name, string expectedId)
        {
            var anime = new Anime { Name = name };
            var result = await _mal.GetId(anime);
            Assert.IsTrue(result);
            Assert.AreEqual(anime.MyAnimeList.Id, expectedId);
        }

        /// <summary>
        ///     Test that the myanimelist id finds the correct anime
        ///     on selected general cases
        /// </summary>
        [Test]
        public async Task FindIdTest()
        {
            // exclamations
            await TestId("Yuri!!! on Ice", "32995");

            // forward slash & colon
            await TestId("Fate/Grand Order: First Order", "34321");

            // long name, colon
            await TestId("Nejimaki Seirei Senki: Tenkyou No Alderamin", "31764");

            // parenthesis
            await TestId("Gi(A)Rlish Number", "32607");

            // semicolon
            await TestId("Occultic;Nine", "32962");

            // number, period, long name, second cour
            await TestId("12-Sai.: Chicchana Mune No Tokimeki 2Nd Season", "33419");

            // reboot, year in title
            await TestId("Berserk (2016)", "32379");

            // very short title, close to other series names, meta information in title
            await TestId("Days (TV)", "32494");

            // weird symbol, meta information
            await TestId("Saiki Kusuo No Ψ-Nan (TV)", "33255");

            // name very close to OVA, meta information in title
            await TestId("Big Order (TV)", "31904");

            // this should be pretty hard to fail
            await TestId("Kiznaiver", "31798");

            // short name
            await TestId("Ajin", "31580");

            // second cour, short name
            await TestId("Gate: Jieitai Kanochi Nite, Kaku Tatakaeri 2Nd Season", "31637");

            // first cour
            await TestId("Fate/Stay Night: Unlimited Blade Works", "22297");
        }
    }
}
