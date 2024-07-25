using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Modrinth.Api.Core.Filter
{
    public class ProjectFilter
    {
        private ICollection<Facet> _facets = new List<Facet>();
        public string Query { get; set; }
        public string Index { get; set; } = FaceIndexEnum.Relevance;
        public Int16 Offset { get; set; }
        public Int16 Limit { get; set; } = 20;

        public Facet AddFacet(string key, string value, LogicalOperator logicalOperator = LogicalOperator.And)
        {
            var element = _facets.FirstOrDefault(c => c.Key == key && c.Value == value);

            if (element != null) return element;

            var facet = new Facet
            {
                Key = key,
                Value = value,
                LogicalOperator = logicalOperator
            };

            _facets.Add(facet);

            return facet;
        }



        public void ToggleFacet(string key, string value, LogicalOperator logicalOperator = LogicalOperator.And)
        {
            var element = _facets.FirstOrDefault(c => c.Key == key && c.Value == value);

            if (element == null)
            {
                AddFacet(key, value, logicalOperator);
            }
            else
            {
                RemoveFacet(key, value);
            }
        }

        public void RemoveFacet(string key, string value)
        {
            var element = _facets.FirstOrDefault(c => c.Key == key && c.Value == value);

            if (element != null)
            {
                _facets.Remove(element);
            }
        }

        internal string ToQueryString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendJoin(string.Empty, "?query=", Query);
            stringBuilder.AppendJoin(string.Empty, "&offset=", Offset);
            stringBuilder.AppendJoin(string.Empty, "&limit=", Limit);
            stringBuilder.AppendJoin(string.Empty, "&index=", Index);

            if (_facets.Count > 0)
            {
                var groupedFacets = _facets.GroupBy(c => new
                {
                    c.Key, c.LogicalOperator
                }).ToList();

                stringBuilder.Append("&facets=[");

                var endOrFacets = groupedFacets
                    .Where(c => c.Key.LogicalOperator == LogicalOperator.Or)
                    .ToList();

                if (endOrFacets.Count > 0)
                {
                    foreach (var endOrFacet in endOrFacets)
                    {
                        stringBuilder.Append("[");
                        stringBuilder.AppendJoin(',', endOrFacet.Select(c => $"\"{c.Key}:{c.Value}\""));
                        stringBuilder.Append("],");
                    }

                    stringBuilder.Length--;
                }


                var endAndFacets = groupedFacets
                    .Where(c => c.Key.LogicalOperator == LogicalOperator.And)
                    .SelectMany(c => c)
                    .ToList();

                if (endAndFacets.Count > 0)
                {
                    if (endOrFacets.Count > 0)
                        stringBuilder.Append(",");

                    stringBuilder.AppendJoin(",", endAndFacets.Select(c => $"[\"{c.Key}:{c.Value}\"]"));
                }

                stringBuilder.Append("]");
            }

            return stringBuilder.ToString();
        }
    }
}
