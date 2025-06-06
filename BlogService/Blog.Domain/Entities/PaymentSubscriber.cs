﻿using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class PaymentSubscriber : IBlogEntity
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionLevelId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        [ForeignKey(nameof(SubscriptionLevelId))]
        public PaymentSubscription SubscriptionLevel { get; set; }

        private PaymentSubscriber() { }

        public PaymentSubscriber(Guid userId, Guid subscriptionLevelId)
        {
            var now = DateTimeService.Now();
            UserId = userId;
            SubscriptionLevelId = subscriptionLevelId;
            CreatedAt = now;
            UpdatedAt = now;
            IsActive = true;
        }
    }
}
