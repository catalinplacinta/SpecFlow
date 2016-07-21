using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TechTalk.SpecFlow.Assist;

namespace TechTalk.SpecFlow.EF
{
    public class EntityFrameworkGiven<T> where T : class
    {
        private readonly DbContext _dbContext;
        private readonly List<Tuple<Expression<Func<T, object>>, object>> _fixedValueExpressions = new List<Tuple<Expression<Func<T, object>>, object>>();
        private readonly List<Tuple<Expression<Func<T, object>>, object>> _defaultValueExpressions = new List<Tuple<Expression<Func<T, object>>, object>>();

        public EntityFrameworkGiven(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public EntityFrameworkGiven<T> FixedValue(Expression<Func<T,object>> propertyExpression, object propertyValue)
        {
            this._fixedValueExpressions.Add(new Tuple<Expression<Func<T, object>>, object>(propertyExpression, propertyValue));

            return this;
        }

        public EntityFrameworkGiven<T> DefaultValue(Expression<Func<T, object>> propertyExpression, object propertyValue)
        {
            this._defaultValueExpressions.Add(new Tuple<Expression<Func<T, object>>, object>(propertyExpression, propertyValue));

            return this;
        }

        public List<T> Execute(Table table)
        {
            var entities = table.CreateSet<T>().ToList();
            foreach (var entity in entities)
            {
                //Set Fixed values
                //Set Default values
                this._dbContext.Set<T>().Add(entity);
            }

            this._dbContext.SaveChanges();

            return entities;
        }

        // Add methods setdefaultValue y setfixedValue
        // Add includes to the constructor, build dependent entities y add them to each instance in execute method
        // Make default and fixed methods work with dependent entities
        // Make identity insertion possible
    }
}
