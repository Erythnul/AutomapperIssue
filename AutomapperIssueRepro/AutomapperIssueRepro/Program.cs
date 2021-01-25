using AutoMapper;
using AutoMapper.QueryableExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomapperIssueRepro
{
    class Program
    {
        private static MapperConfiguration Configuration => new(cfg =>
        {
            cfg.CreateMap<Entities.Order, Dtos.OrderModel>()
                       .ForMember(dst => dst.OrderSubModel, opt => opt.MapFrom(src => src.MostImportantOrderLine != null ? src : null))
                       .ForMember(dst => dst.MostImportantOrderLine, opt =>
                       {
                           opt.MapFrom(src => src.OrderLines.FirstOrDefault(x => x.OrderLineNumber == src.MostImportantOrderLine));
                       });

            cfg.CreateMap<Entities.Order, Dtos.OrderSubModel>()
                .ForMember(dst => dst.OrderId, opt => opt.MapFrom(src => src.Id));

            cfg.CreateMap<Entities.OrderLine, Dtos.OrderLineModel>()
                .ForMember(dst => dst.OrderLineId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dst => dst.Description, opt => opt.MapFrom(src => src.Description));
        });

        private static List<Entities.Order> Orders => new()
        {
            new Entities.Order
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                MostImportantOrderLine = 2,
                OrderLines = new List<Entities.OrderLine>
                {
                    new Entities.OrderLine
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                    OrderId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OrderLineNumber = 2,
                    Description = "ChessSet"
                },
                new Entities.OrderLine
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                    OrderId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OrderLineNumber = 3,
                    Description = "NotAChessSet"
                }
                }
            }
        };

        static void Main(string[] args)
        {
            var queryable = Orders.AsQueryable();

            var projectedQuery = queryable.ProjectTo<Dtos.OrderModel>(Configuration);

            var result = projectedQuery.Single(); //throws

            //projectedQuery.Expression
            queryable.Select(
                dtoOrder => new //Object_1548421275___MostImportantOrderLine_MostImportantOrderLine_MostImportantOrderLine
                {
                    __MostImportantOrderLine = dtoOrder.OrderLines.FirstOrDefault(x => ((int?)x.OrderLineNumber) == dtoOrder.MostImportantOrderLine),
                    MostImportantOrderLine = dtoOrder.MostImportantOrderLine
                    //, Id = dtoOrder.Id
                    // Doesn't collect OrderId into dtoLet, is also not added as possible field to Proxy Object above
                }).Select(
                dtoLet => new Dtos.OrderModel
                {
                    MostImportantOrderLine = (dtoLet.__MostImportantOrderLine == null)
                        ? null
                        : new Dtos.OrderLineModel
                        {
                            Description = dtoLet.__MostImportantOrderLine.Description,
                            OrderLineId = dtoLet.__MostImportantOrderLine.Id
                        },
                    OrderSubModel = (((dtoLet.MostImportantOrderLine != null) ? dtoOrder : null) == null) //Doesn't compile, should be dtoLet
                        ? null
                        : new Dtos.OrderSubModel
                        {
                            OrderId = (Guid?)((dtoLet.MostImportantOrderLine != null) ? dtoOrder : null).Id //Doesn't compile, should be dtoLet
                        }
                });
        }
    }

    public class Entities
    {
        public class Order
        {
            public Guid Id { get; set; }
            public Guid CustomerId { get; set; }

            public int? MostImportantOrderLine { get; set; }

            public ICollection<OrderLine> OrderLines { get; set; } = null!;
        }
        public class OrderLine
        {
            public Guid Id { get; set; }
            public Guid OrderId { get; set; }

            public int OrderLineNumber { get; set; }
            public string Description { get; set; } = null!;

            public Order Order { get; set; } = null!;
        }
    }
    public class Dtos
    {
        public class OrderModel
        {
            public OrderSubModel? OrderSubModel { get; set; }
            public OrderLineModel? MostImportantOrderLine { get; set; }
        }

        public class OrderSubModel
        {
            public Guid? OrderId { get; set; }
        }

        public class OrderLineModel
        {
            public Guid OrderLineId { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}