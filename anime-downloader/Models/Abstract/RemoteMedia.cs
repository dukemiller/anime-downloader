using System;
using System.Linq;
using System.Text.RegularExpressions;
using anime_downloader.Classes;

namespace anime_downloader.Models.Abstract
{
    /// <summary>
    ///     A representation of something that can be initiated
    ///     in some way to retrieve a file.
    /// </summary>
    public abstract class RemoteMedia
    {
        /// <summary>
        ///     The quantified name of the retrievable file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The given contextually dependant remote accesser (download link, irc message, ...).
        /// </summary>
        public string Remote { get; set; }

        /// <summary>
        ///     The date published of the remote accessor, if available.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     An abstract "healthiness" of the media, if by seeders or download count or ...
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        ///     A download/retrieval count for the selected media.
        /// </summary>
        public int Downloads { get; set; }

        /// <summary>
        ///     The assumed episode given the name of the media.
        /// </summary>
        public int Episode
        {
            get
            {
                var strippedName = Methods.Strip(Name);
                var episode = 0;

                if (strippedName.Any(char.IsDigit))
                {
                    if (strippedName.Contains("-"))
                    {
                        var _ = string.Join("",
                            strippedName.Replace(" ", "")
                                .Split('-')
                                .Last(stripped => stripped.Any(char.IsNumber))
                                .TakeWhile(char.IsNumber)
                        );

                        var result = int.TryParse(_, out int number);
                        episode = result ? number : 0;
                    }

                    else
                    {
                        // Work backwords from the last phrase, taking any token that is only numbers
                        var _ = strippedName.Split(' ')
                            .Reverse()
                            .SkipWhile(chunk => !chunk.All(char.IsDigit))
                            .FirstOrDefault();
                        var result = int.TryParse(_, out int number);
                        episode = result ? number : 0;
                    }
                }

                return episode;
            }
        }

        /// <summary>
        ///     Returns the subgroup from the name of the file.
        /// </summary>
        public string Subgroup()
        {
            foreach (Match match in Regex.Matches(Name, @"\[([A-Za-z0-9_µ\s\-]+)\]+"))
            {
                var result = match.Groups[1].Value;
                if (result.All(c => !char.IsNumber(c)))
                    return result;
            }
            return null;
        }

        /// <summary>
        ///     A check if the subgroup exists.
        /// </summary>
        public bool HasSubgroup() => Subgroup() != null;

        public string StrippedName => Methods.Strip(Name);

        public string StrippedWithNoEpisode => Methods.Strip(Name, true);

        /// <summary>
        ///     A simple representation of the important attributes of a Nyaa object.
        /// </summary>
        /// <returns>
        ///     Summary of torrent providers' values
        /// </returns>
        public override string ToString() => $"{GetType().Name}<name={Name}, remote={Remote}>";
    }
}