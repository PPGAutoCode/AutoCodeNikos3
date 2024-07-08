
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
            ValidateCreateFAQRequest(request);

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

            var fAQFAQCategories = new List<FAQFAQCategory>();
            foreach (var categoryId in request.FAQCategories)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new BusinessException("DP-404", "FAQ category not found");
                }

                fAQFAQCategories.Add(new FAQFAQCategory
                {
                    Id = Guid.NewGuid(),
                    FAQId = faq.Id,
                    FAQCategoryId = categoryId
                });
            }

            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO FAQs (Id, Question, Answer, Langcode, Status, FaqOrder, Created, Changed) VALUES (@Id, @Question, @Answer, @Langcode, @Status, @FaqOrder, @Created, @Changed)",
                        faq, transaction);

                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) VALUES (@Id, @FAQId, @FAQCategoryId)",
                        fAQFAQCategories, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator");
                }
            }

            return faq.Id.ToString();
        }

        public async Task<FAQ> GetFAQ(FAQRequestDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Invalid FAQ ID");
            }

            var faq = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (faq == null)
            {
                throw new TechnicalException("DP-404", "FAQ not found");
            }

            var categoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @Id", new { request.Id });

            var categories = new List<FAQCategory>();
            foreach (var categoryId in categoryIds)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "FAQ category not found");
                }
                categories.Add(category);
            }

            faq.FAQCategories = categories.Select(c => c.Id).ToList();
            return faq;
        }

        public async Task<string> UpdateFAQ(UpdateFAQDto request)
        {
            ValidateUpdateFAQRequest(request);

            var existingFAQ = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (existingFAQ == null)
            {
                throw new BusinessException("DP-404", "FAQ not found");
            }

            existingFAQ.Question = request.Question;
            existingFAQ.Answer = request.Answer;
            existingFAQ.Langcode = request.Langcode;
            existingFAQ.Status = request.Status;
            existingFAQ.FaqOrder = request.FaqOrder;
            existingFAQ.Changed = DateTime.UtcNow;

            var existingCategoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId = @Id", new { request.Id });

            var categoriesToRemove = existingCategoryIds.Except(request.FAQCategories).ToList();
            var categoriesToAdd = request.FAQCategories.Except(existingCategoryIds).ToList();

            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    await _dbConnection.ExecuteAsync(
                        "UPDATE FAQs SET Question = @Question, Answer = @Answer, Langcode = @Langcode, Status = @Status, FaqOrder = @FaqOrder, Changed = @Changed WHERE Id = @Id",
                        existingFAQ, transaction);

                    if (categoriesToRemove.Any())
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM FAQFAQCategories WHERE FAQId = @Id AND FAQCategoryId IN @CategoriesToRemove",
                            new { request.Id, CategoriesToRemove = categoriesToRemove }, transaction);
                    }

                    foreach (var categoryId in categoriesToAdd)
                    {
                        var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                        var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                        if (category == null)
                        {
                            throw new BusinessException("DP-404", "FAQ category not found");
                        }

                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO FAQFAQCategories (Id, FAQId, FAQCategoryId) VALUES (@Id, @FAQId, @FAQCategoryId)",
                            new { Id = Guid.NewGuid(), FAQId = request.Id, FAQCategoryId = categoryId }, transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator");
                }
            }

            return existingFAQ.Id.ToString();
        }

        public async Task<bool> DeleteFAQ(DeleteFAQDto request)
        {
            ValidateDeleteFAQRequest(request);

            var existingFAQ = await _dbConnection.QuerySingleOrDefaultAsync<FAQ>(
                "SELECT * FROM FAQs WHERE Id = @Id", new { request.Id });

            if (existingFAQ == null)
            {
                throw new BusinessException("DP-404", "FAQ not found");
            }

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
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator");
                }
            }

            return true;
        }

        public async Task<List<FAQ>> GetListFAQ(ListFAQRequestDto request)
        {
            ValidateListFAQRequest(request);

            var query = "SELECT * FROM FAQs";
            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                query += $" ORDER BY {request.SortField} {request.SortOrder}";
            }
            query += " OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            var faqs = await _dbConnection.QueryAsync<FAQ>(query, new { request.PageOffset, request.PageLimit });

            if (faqs.Count() == 0)
            {
                throw new TechnicalException("DP-400", "No FAQs found");
            }

            var faqIds = faqs.Select(f => f.Id).ToList();
            var categoryIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT FAQCategoryId FROM FAQFAQCategories WHERE FAQId IN @Ids", new { Ids = faqIds });

            var categories = new List<FAQCategory>();
            foreach (var categoryId in categoryIds)
            {
                var categoryRequest = new FAQCategoryRequestDto { Id = categoryId };
                var category = await _faqCategoryService.GetFAQCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "FAQ category not found");
                }
                categories.Add(category);
            }

            foreach (var faq in faqs)
            {
                faq.FAQCategories = categories.Where(c => categoryIds.Contains(c.Id)).Select(c => c.Id).ToList();
            }

            return faqs.ToList();
        }

        private void ValidateCreateFAQRequest(CreateFAQDto request)
        {
            if (string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.Answer) ||
                string.IsNullOrEmpty(request.Langcode) || request.FaqOrder == 0)
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }
        }

        private void ValidateUpdateFAQRequest(UpdateFAQDto request)
        {
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.Answer) ||
                string.IsNullOrEmpty(request.Langcode) || request.FaqOrder == 0)
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }
        }

        private void ValidateDeleteFAQRequest(DeleteFAQDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }
        }

        private void ValidateListFAQRequest(ListFAQRequestDto request)
        {
            if (request.PageLimit == 0 || request.PageOffset == 0)
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }
        }
    }
}
