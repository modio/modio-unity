using System.Collections.Generic;

namespace Modio.Search
{
    public interface IModioSearchCarouselProvider
    {
        public IReadOnlyCollection<(IModioSearchSettings settings, CarouselStyle style)> Carousels { get; set; }
    }
}
