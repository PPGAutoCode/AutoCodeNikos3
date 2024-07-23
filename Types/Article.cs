
using System;
using System.Collections.Generic;

namespace ProjectName.Types
{
    public class Article
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Guid Author { get; set; }
        public string Summary { get; set; }
        public string Body { get; set; }
        public string GoogleDriveID { get; set; }
        public bool HideScrollSpy { get; set; }
        public Guid? Image { get; set; }
        public Guid? PDF { get; set; }
        public string Langcode { get; set; }
        public bool Status { get; set; }
        public bool Sticky { get; set; }
        public bool Promote { get; set; }
        public List<Guid> BlogCategories { get; set; }
        public List<BlogTag> BlogTags { get; set; }
        public int? Version { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Changed { get; set; }
        public Guid CreatorId { get; set; }
        public Guid? ChangedUser { get; set; }
    }
}
