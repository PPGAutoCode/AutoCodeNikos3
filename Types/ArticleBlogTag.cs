
using System;

namespace ProjectName.Types
{
    public class ArticleBlogTag
    {
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public Guid BlogTagId { get; set; }
    }
}
