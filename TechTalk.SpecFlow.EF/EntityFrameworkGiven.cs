using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecFlow;

namespace TechTalk.SpecFlow.EF
{
    public class EntityFrameworkGiven<T> where T : class
    {
        private DbContext dbContext;
        private List<Tuple<Expression<Func<T, object>>, object>> fixedValueExpressions = new List<Tuple<Expression<Func<T, object>>, object>>();
        private List<Tuple<Expression<Func<T, object>>, object>> defaultValueExpressions = new List<Tuple<Expression<Func<T, object>>, object>>();

        public EntityFrameworkGiven(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public EntityFrameworkGiven<T> FixedValue(Expression<Func<T,object>> propertyExpression, object propertyValue)
        {
            this.fixedValueExpressions.Add(new Tuple<Expression<Func<T, object>>, object>(propertyExpression, propertyValue));
            return this;
        }

        public EntityFrameworkGiven<T> DefaultValue(Expression<Func<T, object>> propertyExpression, object propertyValue)
        {
            this.defaultValueExpressions.Add(new Tuple<Expression<Func<T, object>>, object>(propertyExpression, propertyValue));
            return this;
        }

        public List<T> Execute(Table table)
        {
            List<T> entities = table.CreateSet<T>().ToList();
            foreach (T entity in entities)
            {
                //Set Fixed values
                //Set Default values
                this.dbContext.Set<T>().Add(entity);
            }

            this.dbContext.SaveChanges();
            return entities;
        }

        // Add methods setdefaultValue y setfixedValue
        // Add includes to the constructor, build dependent entities y add them to each instance in execute method
        // Make default and fixed methods work with dependent entities
        // Make identity insertion possible
    }
}
