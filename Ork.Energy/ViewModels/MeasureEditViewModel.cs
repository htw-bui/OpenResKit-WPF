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

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Ork.Energy.Domain.DomainModelService;
using Ork.Energy.Factories;
using Ork.Framework;

namespace Ork.Energy.ViewModels
{
  public class MeasureEditViewModel : MeasureAddViewModel
  {
    private readonly Action m_RemoveMeasure;
    private readonly IEnumerable m_Stati;

    [ImportingConstructor]
    public MeasureEditViewModel(EnergyMeasure model, Action removeMeasureAction, ResponsibleSubjectViewModel[] responsibleSubjectViewModels, [Import] IEnergyRepository energyRepository,
      [Import] IViewModelFactory subMeasureViewModelFactory)
      : base(model, responsibleSubjectViewModels, energyRepository, subMeasureViewModelFactory)
    {
      DisplayName = TranslationProvider.Translate("TitleMeasureEditViewModel");
      m_Stati = Enum.GetValues(typeof (Status));
      m_RemoveMeasure = removeMeasureAction;
      SelectedResponsibleSubject = responsibleSubjectViewModels.Single(rsvm => model.ResponsibleSubject == rsvm.Model);
      foreach (var sm in energyRepository.SubMeasures.Where(sm => sm.ReleatedMeasure == model))
      {
        sm.PropertyChanged += ListenToIsCompleted;
      }
      energyRepository.SubMeasures.CollectionChanged += (s, e) =>
                                                        {
                                                          switch (e.Action)
                                                          {
                                                            case NotifyCollectionChangedAction.Add:
                                                              foreach (var newItem in e.NewItems.OfType<SubMeasure>())
                                                              {
                                                                if (newItem.ReleatedMeasure == model)
                                                                {
                                                                  newItem.PropertyChanged += ListenToIsCompleted;
                                                                }
                                                              }
                                                              break;
                                                            case NotifyCollectionChangedAction.Remove:
                                                              foreach (var oldItem in e.OldItems.OfType<SubMeasure>())
                                                              {
                                                                if (oldItem.ReleatedMeasure == model)
                                                                {
                                                                  oldItem.PropertyChanged -= ListenToIsCompleted;
                                                                }
                                                              }
                                                              break;
                                                          }
                                                        };
    }


    public double EvaluationValue
    {
      get { return Model.EvaluationRating; }
      set
      {
        Model.EvaluationRating = value;
        NotifyOfPropertyChange(() => EvaluationValue);
      }
    }

    public string Evaluation
    {
      get { return Model.Evaluation; }
      set { Model.Evaluation = value; }
    }

    public DateTime? EntryDate
    {
      get { return Model.EntryDate; }
      set { Model.EntryDate = value.Value; }
    }

    public string EntryDateString
    {
      get
      {
        if (Model.EntryDate != null)
        {
          return Model.EntryDate.Value.ToShortDateString();
        }
        return null;
      }
    }

    public int Status
    {
      get { return Model.Status; }
      set { Model.Status = value; }
    }

    public string StatusName
    {
      get { return TranslationProvider.Translate(((Status) Status).ToString()); }
    }

    public IEnumerable Stati
    {
      get
      {
        if (SubMeasures.Any(sm => !sm.IsCompleted))
        {
          return m_Stati.OfType<object>()
                        .Take(2);
        }
        else
        {
          return m_Stati;
        }
      }
    }

    private void ListenToIsCompleted(object s, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsCompleted")
      {
        NotifyOfPropertyChange(() => Stati);
      }
    }

    public void RemoveMeasures()
    {
      m_RemoveMeasure();
    }
  }
}