
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.ControllersExceptions;
using ProjectName.Interfaces;
using ProjectName.Types;

namespace ProjectName.Services
{
    public class FAQService : IFAQService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IFAQCategoryService _faqCategoryService;

        public FAQService(IDbConnection dbConnection, IFAQCategoryService faqCategoryService)
        {
            _dbConnection = dbConnection;
            _faqCategoryService = faqCategoryService;
        }

        public async Task<string> CreateFAQ(CreateFAQDto request)
        {
            // Step 1: Validate the request payload
            if (string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.Answer) ||
                string.IsNullOrEmpty(request.Langcode) || request.FaqOrder == 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch FAQ category details
            var faqCategories = new List<Guid>();
            foreach (var categoryId in request.FAQCategories)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category.Id);
            }

            // Step 3: Create a new FAQ object
            var faq = new FAQ
            {
                Id = Guid.NewGuid(),
                Question = request.Question,
                Answer = request.Answer,
                Langcode = request.Langcode,
                Status = request.Status,
                FaqOrder = request.FaqOrder,
                Created = DateTime.UtcNow,
                Changed = DateTime.UtcNow
            };

            // Step 4: Create new list of FAQFAQCategories type objects
            var fAQFAQCategories = request.FAQCategories.Select(categoryId => new FAQFAQCategory
            {
                Id = Guid.NewGuid(),
                FAQId = faq.Id,
                FAQCategoryId = categoryId
            }).ToList();

            // Step 5: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Insert faq in database table FAQs
                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO FAQs (Id, Question, Answer, Langcode, Status, FaqOrder, Created, Changed) " +
                        "VALUES (@Id, @Question, @Answer, @Langcode, @Status, @FaqOrder, @Created, @Changed)",
                        faq, transaction);

                    // Insert fAQFAQCategories in database table FAQFAQCategories
                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) VALUES (@Id, @FAQId, @FAQCategoryId)",
                        fAQFAQCategories, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return faq.Id.ToString();
        }

        public async Task<FAQ> GetFAQ(FAQRequestDto request)
        {
            // Step 1: Validate the request payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch faq from FAQs database table by id
            var faq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (faq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Faq Categories
            var faqCategoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @Id", new { request.Id });

            var faqCategories = new List<FAQCategory>();
            foreach (var categoryId in faqCategoryIds)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category);
            }

            faq.FAQCategories = faqCategories.Select(c => c.Id).ToList();
            return faq;
        }

        public async Task<string> UpdateFAQ(UpdateFAQDto request)
        {
            // Step 1: Validate the request payload
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.Answer) ||
                string.IsNullOrEmpty(request.Langcode) || request.FaqOrder == 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the existing FAQ object from the database
            var existingFaq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (existingFaq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and validate FAQ Categories
            var existingCategoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @Id", new { request.Id });

            var categoriesToRemove = existingCategoryIds.Except(request.FAQCategories).ToList();
            var categoriesToAdd = request.FAQCategories.Except(existingCategoryIds).ToList();

            foreach (var categoryId in categoriesToAdd)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
            }

            // Step 4: Update the FAQ object
            existingFaq.Question = request.Question;
            existingFaq.Answer = request.Answer;
            existingFaq.Langcode = request.Langcode;
            existingFaq.Status = request.Status;
            existingFaq.FaqOrder = request.FaqOrder;
            existingFaq.Changed = DateTime.UtcNow;

            // Step 5: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Update the FAQ in the database
                    await _dbConnection.ExecuteAsync(
                        "UPDATE FAQs SET Question = @Question, Answer = @Answer, Langcode = @Langcode, " +
                        "Status = @Status, FaqOrder = @FaqOrder, Changed = @Changed WHERE Id = @Id",
                        existingFaq, transaction);

                    // Remove old categories
                    if (categoriesToRemove.Any())
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM FAQFAQCategories WHERE FAQId = @Id AND FAQCategoryId IN @CategoriesToRemove",
                            new { request.Id, CategoriesToRemove = categoriesToRemove }, transaction);
                    }

                    // Add new categories
                    var newCategories = categoriesToAdd.Select(categoryId => new FAQFAQCategory
                    {
                        Id = Guid.NewGuid(),
                        FAQId = request.Id,
                        FAQCategoryId = categoryId
                    }).ToList();

                    if (newCategories.Any())
                    {
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) VALUES (@Id, @FAQId, @FAQCategoryId)",
                            newCategories, transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return existingFaq.Id.ToString();
        }

        public async Task<bool> DeleteFAQ(DeleteFAQDto request)
        {
            // Step 1: Validate the request payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the existing FAQ object from the database
            var existingFaq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (existingFaq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the FAQ object from the database
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM FAQFAQCategories WHERE FAQId = @Id", new { request.Id }, transaction);

                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM FAQs WHERE Id = @Id", new { request.Id }, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return true;
        }

        public async Task<List<FAQ>> GetListFAQ(ListFAQRequestDto request)
        {
            // Step 1: Validate Input
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch FAQs
            var query = "SELECT * FROM FAQs";
            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                query += $" ORDER BY {request.SortField} {request.SortOrder}";
            }
            query += " OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            var faqs = await _dbConnection.QueryAsync<FAQ>(query, new { request.PageOffset, request.PageLimit });

            if (faqs == null || !faqs.Any())
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Related FAQ Categories
            var faqIds = faqs.Select(f => f.Id).ToList();
            var faqCategoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId IN @Ids", new { Ids = faqIds });

            var faqCategories = new List<FAQCategory>();
            foreach (var categoryId in faqCategoryIds)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category);
            }

            // Step 4: Response Preparation
            foreach (var faq in faqs)
            {
                faq.FAQCategories = faqCategories.Where(c => c.Id == faq.Id).Select(c => c.Id).ToList();
            }

            return faqs.ToList();
        }
    }
}
