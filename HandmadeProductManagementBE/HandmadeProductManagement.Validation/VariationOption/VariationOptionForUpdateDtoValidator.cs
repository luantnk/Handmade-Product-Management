﻿using FluentValidation;
using HandmadeProductManagement.ModelViews.VariationModelViews;
using HandmadeProductManagement.ModelViews.VariationOptionModelViews;

namespace HandmadeProductManagement.Validation.VariationOption
{
    public class VariationOptionForUpdateDtoValidator : AbstractValidator<VariationOptionForUpdateDto>
    {
        public VariationOptionForUpdateDtoValidator() 
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .MaximumLength(100).WithMessage("Value cannot exceed 100 characters.");
        }
    }
}