
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing article-related operations.
    /// </summary>
    public interface IArticleService
    {
        /// <summary>
        /// Creates a new article based on the provided data.
        /// </summary>
        /// <param name="createArticleDto">Data transfer object containing the information needed to create an article.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateArticle(CreateArticleDto createArticleDto);

        /// <summary>
        /// Retrieves an article based on the provided request data.
        /// </summary>
        /// <param name="articleRequestDto">Data transfer object containing the information needed to retrieve an article.</param>
        /// <returns>An Article object representing the retrieved article.</returns>
        Task<Article> GetArticle(ArticleRequestDto articleRequestDto);

        /// <summary>
        /// Updates an existing article based on the provided data.
        /// </summary>
        /// <param name="updateArticleDto">Data transfer object containing the information needed to update an article.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateArticle(UpdateArticleDto updateArticleDto);

        /// <summary>
        /// Deletes an article based on the provided data.
        /// </summary>
        /// <param name="deleteArticleDto">Data transfer object containing the information needed to delete an article.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        Task<bool> DeleteArticle(DeleteArticleDto deleteArticleDto);

        /// <summary>
        /// Retrieves a list of articles based on the provided request data.
        /// </summary>
        /// <param name="listArticleRequestDto">Data transfer object containing the information needed to retrieve a list of articles.</param>
        /// <returns>A list of Article objects representing the retrieved articles.</returns>
        Task<List<Article>> GetListArticle(ListArticleRequestDto listArticleRequestDto);
    }
}
