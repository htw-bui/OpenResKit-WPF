﻿#region License

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0.html
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  
// Copyright (c) 2013, HTW Berlin

#endregion

using System.ComponentModel.Composition;
using Ork.Invest.DomainModelService;
using Ork.Invest.ViewModels;

namespace Ork.Invest.Factories
{
  [Export(typeof (IComparisonViewModelFactory))]
  internal class ComparisonViewModelFactory : IComparisonViewModelFactory
  {
    public ComparisonViewModel CreateFromExisting(Comparison comparison)
    {
      return new ComparisonViewModel(comparison);
    }

    public ComparisonAddViewModel CreateComparisonAddViewModel(InvestmentPlan investmentPlan)
    {
      return new ComparisonAddViewModel(investmentPlan, new Comparison());
    }

    public ComparisonEditViewModel CreateComparisonEditViewModel(Comparison comparison, InvestmentPlan investmentPlan)
    {
      return new ComparisonEditViewModel(investmentPlan, comparison);
    }
  }
}