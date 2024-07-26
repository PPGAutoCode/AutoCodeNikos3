
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Interfaces;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

namespace ProjectName.Services
{
    public class FAQCategoryService : IFAQCategoryService
    {
        private readonly IDbConnection _dbConnection;

        public FAQCategoryService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateFAQCategory(CreateFAQCategoryDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var faqCategory = new FAQCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description
            };

            const string sql = "INSERT INTO FAQCategories (Id, Name, Description) VALUES (@Id, @Name, @Description)";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, faqCategory);

            if (affectedRows > 0)
            {
                return faqCategory.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<FAQCategory> GetFAQCategory(FAQCategoryRequestDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string sql = "SELECT * FROM FAQCategories WHERE Id = @Id";
            var faqCategory = await _dbConnection.QuerySingleOrDefaultAsync<FAQCategory>(sql, new { request.Id });

            if (faqCategory != null)
            {
                return faqCategory;
            }
            else
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }
        }

        public async Task<string> UpdateFAQCategory(UpdateFAQCategoryDto request)
        {
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM FAQCategories WHERE Id = @Id";
            var existingCategory = await _dbConnection.QuerySingleOrDefaultAsync<FAQCategory>(selectSql, new { request.Id });

            if (existingCategory == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingCategory.Name = request.Name;
            existingCategory.Description = request.Description ?? existingCategory.Description;

            const string updateSql = "UPDATE FAQCategories SET Name = @Name, Description = @Description WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(updateSql, existingCategory);

            if (affectedRows > 0)
            {
                return existingCategory.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteFAQCategory(DeleteFAQCategoryDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM FAQCategories WHERE Id = @Id";
            var existingCategory = await _dbConnection.QuerySingleOrDefaultAsync<FAQCategory>(selectSql, new { request.Id });

            if (existingCategory == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string deleteSql = "DELETE FROM FAQCategories WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(deleteSql, new { request.Id });

            if (affectedRows > 0)
            {
                return true;
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<FAQCategory>> GetListFAQCategory(ListFAQCategoryRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sql = "SELECT * FROM FAQCategories";

            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                sql += $" ORDER BY {request.SortField} {request.SortOrder}";
            }

            sql += " OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            var faqCategories = await _dbConnection.QueryAsync<FAQCategory>(sql, new { request.PageOffset, request.PageLimit });

            if (faqCategories != null)
            {
                return faqCategories.AsList();
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
