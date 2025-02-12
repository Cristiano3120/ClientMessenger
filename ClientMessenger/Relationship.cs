﻿using System.Collections;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public class Relationship: IEnumerable<(string name, string value)>
    {
        public long Id { get; init; } = -1;
        public BitmapImage? ProfilePicture { get; set; }
        public string Username { get; set; } = "";
        public string HashTag { get; set; } = "";
        public string Biography { get; set; } = "";
        public Relationshipstate Relationshipstate { get; set; }

        public IEnumerator<(string name, string value)> GetEnumerator()
        {
            yield return (nameof(Username), Username);
            yield return (nameof(HashTag), HashTag);
            yield return (nameof(Biography), Biography);
            yield return (nameof(Id), Id.ToString());
            if (ProfilePicture != null)
            {
                yield return (nameof(ProfilePicture), Convert.ToBase64String(Converter.ToByteArray(ProfilePicture)));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
