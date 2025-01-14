﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucinq.Core.Querying;
using Lucinq.Lucene30.Adapters;

namespace Lucinq.Lucene30.Querying
{
	public class LuceneSearchResult : ILuceneSearchResult
    {
        #region [ Fields ]

	    private readonly Query query;
	    private Sort sort;
	    private int totalHits;
	    private bool searchExecuted;
	    private TopDocs topDocs;
	    private readonly IIndexSearcherAccessor searcherAccessor;

		private readonly Filter filter;

        #endregion

        #region [ Constructors ]

        public LuceneSearchResult(IIndexSearcherAccessor searcherAccessor, LuceneModel model)
        {
            query = model.Query;
            sort = model.Sort;
            this.searcherAccessor = searcherAccessor;
            filter = model.Filter;
        }

		#endregion

		#region [ Properties ]

		public int TotalHits
		{
		    get
		    {
                ExecuteSearch();
		        return totalHits;
		    }
		}

		public long ElapsedTimeMs { get; set; }

        protected List<Document> Documents { get; set; }

        #endregion

        #region [ Methods ]

        public virtual IList<Document> GetTopItems()
		{
            using (var indexSearcherProvider = searcherAccessor.GetIndexSearcherProvider())
		    {
		        ExecuteSearch(indexSearcherProvider);
		        if (Documents != null)
		        {
		            return Documents;
		        }
		        return topDocs == null
		            ? null
		            : (from ScoreDoc doc in topDocs.ScoreDocs select GetDocument(doc.Doc, indexSearcherProvider.IndexSearcher)).ToList();
		    }
		}

        /// <summary>
        /// Gets a range of items on a zero based index
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
		public virtual IList<Document> GetRange(int start, int end)
		{
            using (var indexSearcherProvider = searcherAccessor.GetIndexSearcherProvider())
		    {
		        ExecuteSearch(indexSearcherProvider);
		        if (start < 0)
		        {
		            start = 0;
		        }

		        if (Documents != null)
		        {
		            end = end + 1;
		            int take = end - start;
                    return Documents.Skip(start).Take(take).ToList();
		        }

                if (end > topDocs.TotalHits - 1)
                {
                    end = topDocs.TotalHits - 1;
                }
                if (end > topDocs.ScoreDocs.Length)
                {
                    end = topDocs.ScoreDocs.Length - 1;
                }

		        List<Document> documents = new List<Document>();
		        for (var i = start; i <= end; i++)
		        {

		            {
		                documents.Add(GetDocument(topDocs.ScoreDocs[i].Doc, indexSearcherProvider.IndexSearcher));
		            }
		        }

		        return documents;
		    }
		}

		protected virtual Document GetDocument(int documentId, IndexSearcher indexSearcher)
		{
			return indexSearcher.Doc(documentId);
		}

        private void ExecuteSearch(IIndexSearcherProvider indexSearcherProvider = null)
	    {
            if (searchExecuted)
            {
                return;
            }
            searchExecuted = true;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (sort == null)
            {
                sort = Sort.RELEVANCE;
            }

            if (indexSearcherProvider == null)
            {
                using (var tempSearcherProvider = searcherAccessor.GetIndexSearcherProvider())
                {
                    topDocs = tempSearcherProvider.IndexSearcher.Search(query, filter, int.MaxValue, sort);
                    PopulateDocuments(tempSearcherProvider);
                }
            }
            else
            {
                topDocs = indexSearcherProvider.IndexSearcher.Search(query, filter, int.MaxValue, sort);
                PopulateDocuments(indexSearcherProvider);
            }
            totalHits = topDocs.TotalHits;
            stopwatch.Stop();
	        ElapsedTimeMs = stopwatch.ElapsedMilliseconds;
	    }

	    private void PopulateDocuments(IIndexSearcherProvider tempSearcherProvider)
	    {
	        if (!tempSearcherProvider.ClosesDirectory)
	        {
	            return;
	        }

	        Documents = new List<Document>();
	        foreach (var scoreDoc in topDocs.ScoreDocs)
	        {
	            Documents.Add(GetDocument(scoreDoc.Doc, tempSearcherProvider.IndexSearcher));
	        }
	    }

        #endregion

        #region [ IEnumerable Methods ]

        public IEnumerator<Document> GetEnumerator()
        {
            return GetTopItems().GetEnumerator();
	    }

	    IEnumerator IEnumerable.GetEnumerator()
	    {
	        return GetEnumerator();
        }

        #endregion
    }
}
