
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
        /// Asynchronously creates a new article.
        /// </summary>
        /// <param name="createArticleDto">The data transfer object containing the information for the new article.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the created article as a string.</returns>
        Task<string> CreateArticle(CreateArticleDto createArticleDto);

        // /// <summary>
        // /// Asynchronously retrieves an article based on the provided request data.
        // /// </summary>
        // /// <param name="articleRequestDto">The data transfer object containing the request information for the article.</param>
        // /// <returns>A task that represents the asynchronous operation. The task result contains the requested article.</returns>
        // Task<Article> GetArticle(ArticleRequestDto articleRequestDto);
        //
        // /// <summary>
        // /// Asynchronously updates an existing article.
        // /// </summary>
        // /// <param name="updateArticleDto">The data transfer object containing the updated information for the article.</param>
        // /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the updated article as a string.</returns>
        // Task<string> UpdateArticle(UpdateArticleDto updateArticleDto);
        //
        // /// <summary>
        // /// Asynchronously deletes an article based on the provided deletion data.
        // /// </summary>
        // /// <param name="deleteArticleDto">The data transfer object containing the information for the article to be deleted.</param>
        // /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating the success of the deletion operation.</returns>
        // Task<bool> DeleteArticle(DeleteArticleDto deleteArticleDto);
        //
        // /// <summary>
        // /// Asynchronously retrieves a list of articles based on the provided request data.
        // /// </summary>
        // /// <param name="listArticleRequestDto">The data transfer object containing the request information for the list of articles.</param>
        // /// <returns>A task that represents the asynchronous operation. The task result contains a list of the requested articles.</returns>
        // Task<List<Article>> GetListArticle(ListArticleRequestDto listArticleRequestDto);
    }
}
