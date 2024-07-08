
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
            var faqCategories = new List<FAQCategory>();
            foreach (var categoryId in request.FAQCategories)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category);
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
            var fAQFAQCategories = faqCategories.Select(category => new FAQFAQCategory
            {
                Id = Guid.NewGuid(),
                FAQId = faq.Id,
                FAQCategoryId = category.Id
            }).ToList();

            // Step 5: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Insert faq in database table FAQs
                    var insertFaqQuery = @"INSERT INTO FAQs (Id, Question, Answer, Langcode, Status, FaqOrder, Created, Changed) 
                                           VALUES (@Id, @Question, @Answer, @Langcode, @Status, @FaqOrder, @Created, @Changed)";
                    await _dbConnection.ExecuteAsync(insertFaqQuery, faq, transaction);

                    // Insert fAQFAQCategories in database table FAQFAQCategories
                    var insertFaqFaqCategoriesQuery = @"INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) 
                                                        VALUES (@Id, @FAQId, @FAQCategoryId)";
                    await _dbConnection.ExecuteAsync(insertFaqFaqCategoriesQuery, fAQFAQCategories, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            // Step 6: Return FAQ id from the database
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
            var faqQuery = @"SELECT * FROM FAQs WHERE Id = @Id";
            var faq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(faqQuery, new { request.Id });

            if (faq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Faq Categories
            var faqCategoryIdsQuery = @"SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @FAQId";
            var faqCategoryIds = await _dbConnection.QueryAsync<Guid>(faqCategoryIdsQuery, new { FAQId = faq.Id });

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
            var faqQuery = @"SELECT * FROM FAQs WHERE Id = @Id";
            var existingFaq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(faqQuery, new { request.Id });

            if (existingFaq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and validate FAQ Categories
            var existingFaqCategoryIdsQuery = @"SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @FAQId";
            var existingFaqCategoryIds = await _dbConnection.QueryAsync<Guid>(existingFaqCategoryIdsQuery, new { FAQId = existingFaq.Id });

            var faqCategories = new List<FAQCategory>();
            for categoryId in request.FAQCategories:
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category);

            // Step 4: Update FAQ Categories
            var categoriesToRemove = existingFaqCategoryIds.Except(request.FAQCategories).ToList();
            var categoriesToAdd = request.FAQCategories.Except(existingFaqCategoryIds).ToList();

            using (var transaction = _dbConnection.BeginTransaction())
            {
                try:
                    // Remove Old Categories
                    if (categoriesToRemove.Any())
                    {
                        var deleteFaqFaqCategoriesQuery = @"DELETE FROM FAQFAQCategories WHERE FAQId = @FAQId AND FAQCategoryId = @FAQCategoryId";
                        await _dbConnection.ExecuteAsync(deleteFaqFaqCategoriesQuery, categoriesToRemove.Select(id => new { FAQId = existingFaq.Id, FAQCategoryId = id }), transaction);
                    }

                    // Add New Categories
                    var newFaqFaqCategories = categoriesToAdd.Select(id => new FAQFAQCategory
                    {
                        Id = Guid.NewGuid(),
                        FAQId = existingFaq.Id,
                        FAQCategoryId = id
                    }).ToList();

                    var insertFaqFaqCategoriesQuery = @"INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) 
                                                        VALUES (@Id, @FAQId, @FAQCategoryId)";
                    await _dbConnection.ExecuteAsync(insertFaqFaqCategoriesQuery, newFaqFaqCategories, transaction);

                    // Update the FAQ object
                    existingFaq.Question = request.Question;
                    existingFaq.Answer = request.Answer;
                    existingFaq.Langcode = request.Langcode;
                    existingFaq.Status = request.Status;
                    existingFaq.FaqOrder = request.FaqOrder;
                    existingFaq.Changed = DateTime.UtcNow;

                    var updateFaqQuery = @"UPDATE FAQs 
                                           SET Question = @Question, Answer = @Answer, Langcode = @Langcode, 
                                               Status = @Status, FaqOrder = @FaqOrder, Changed = @Changed 
                                           WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateFaqQuery, existingFaq, transaction);

                    transaction.Commit();
                except Exception:
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
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
            var faqQuery = @"SELECT * FROM FAQs WHERE Id = @Id";
            var existingFaq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(faqQuery, new { request.Id });

            if (existingFaq == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the FAQ object from the database
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try:
                    var deleteFaqFaqCategoriesQuery = @"DELETE FROM FAQFAQCategories WHERE FAQId = @FAQId";
                    await _dbConnection.ExecuteAsync(deleteFaqFaqCategoriesQuery, new { FAQId = existingFaq.Id }, transaction);

                    var deleteFaqQuery = @"DELETE FROM FAQs WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(deleteFaqQuery, new { existingFaq.Id }, transaction);

                    transaction.Commit();
                except Exception:
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
            }

            return True;
        }

        public async Task<List<FAQ>> GetListFAQ(ListFAQRequestDto request)
        {
            // Step 1: Validate Input
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch FAQs
            var faqsQuery = @"SELECT * FROM FAQs 
                              ORDER BY 
                              CASE WHEN @SortField IS NOT NULL AND @SortOrder = 'ASC' THEN @SortField END ASC,
                              CASE WHEN @SortField IS NOT NULL AND @SortOrder = 'DESC' THEN @SortField END DESC
                              OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            var faqs = await _dbConnection.QueryAsync<FAQ>(faqsQuery, new { request.SortField, request.SortOrder, request.PageOffset, request.PageLimit });

            if (faqs == null or not faqs.Any())
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Related FAQ Categories
            var faqIds = faqs.Select(f => f.Id).ToList();
            var faqCategoryIdsQuery = @"SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId IN @FAQIds";
            var faqCategoryIds = await _dbConnection.QueryAsync<Guid>(faqCategoryIdsQuery, new { FAQIds = faqIds });

            var faqCategories = new List<FAQCategory>();
            for categoryId in faqCategoryIds:
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                faqCategories.Add(category);

            // Step 4: Response Preparation
            for faq in faqs:
                faq.FAQCategories = faqCategories.Where(c => c.Id == faq.Id).Select(c => c.Id).ToList();

            return faqs.ToList();
        }
    }
}
