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
using System.ComponentModel.Composition;
using System.Data.Services.Client;
using Ork.Discard.DomainModelService;
using Ork.Setting;

namespace Ork.Discard
{
  [Export(typeof (IDiscardRepository))]
  internal class DiscardRepository : IDiscardRepository
  {
    private readonly Func<DomainModelContext> m_CreateMethod;
    private DomainModelContext m_Context;

    [ImportingConstructor]
    public DiscardRepository([Import] ISettingsProvider settingsContainer, [Import] Func<DomainModelContext> createMethod)
    {
      m_CreateMethod = createMethod;
      settingsContainer.ConnectionStringUpdated += (s, e) => Initialize();
      Initialize();
    }

    public bool HasConnection { get; private set; }

    public DataServiceCollection<ResponsibleSubject> ResponsibleSubjects { get; private set; }
    public DataServiceCollection<InspectionAttribute> InspectionAttributes { get; private set; }
    public DataServiceCollection<ProductionItem> ProductionItems { get; private set; }
    public DataServiceCollection<Inspection> Inspections { get; private set; }
    public DataServiceCollection<Customer> Customers { get; private set; }
    public event EventHandler ContextChanged;
    public event EventHandler SaveCompleted;

    public void Save()
    {
      if (m_Context.ApplyingChanges)
      {
        return;
      }

      var result = m_Context.BeginSaveChanges(SaveChangesOptions.Batch, r =>
                                                                        {
                                                                          var dm = (DomainModelContext) r.AsyncState;
                                                                          dm.EndSaveChanges(r);
                                                                          RaiseEvent(SaveCompleted);
                                                                        }, m_Context);
    }


    private void Initialize()
    {
      m_Context = m_CreateMethod();

      try
      {
        LoadResponsibleSubjects();
        LoadInspections();
        LoadInspectionAttributes();
        LoadProductionItems();
        LoadCustomers();
        HasConnection = true;
      }
      catch
      {
        HasConnection = false;
      }

      RaiseEvent(ContextChanged);
    }

    private void LoadProductionItems()
    {
      ProductionItems = new DataServiceCollection<ProductionItem>(m_Context);
      var query = m_Context.ProductionItems.Expand("InspectionAttributes")
                           .Expand("Customer")
                           .Expand("InspectionAttributes/DiscardImageSource");
      ProductionItems.Load(query);
    }

    private void LoadInspectionAttributes()
    {
      InspectionAttributes = new DataServiceCollection<InspectionAttribute>(m_Context);
      var query = m_Context.InspectionAttributes;
      InspectionAttributes.Load(query);
    }

    private void LoadInspections()
    {
      Inspections = new DataServiceCollection<Inspection>(m_Context);
      var query = m_Context.Inspections.Expand("ProductionItem")
                           .Expand("DiscardItems")
                           .Expand("ResponsibleSubject")
                           .Expand("ProductionItem/InspectionAttributes")
                           .Expand("DiscardItems/InspectionAttribute");
      Inspections.Load(query);
    }

    private void LoadResponsibleSubjects()
    {
      ResponsibleSubjects = new DataServiceCollection<ResponsibleSubject>(m_Context);

      var query = m_Context.ResponsibleSubjects.Expand("OpenResKit.DomainModel.Employee/Groups");
      ResponsibleSubjects.Load(query);
    }

    private void LoadCustomers()
    {
      Customers = new DataServiceCollection<Customer>(m_Context);
      var query = m_Context.Customers;
      Customers.Load(query);
    }


    private void RaiseEvent(EventHandler eventHandler)
    {
      if (eventHandler != null)
      {
        eventHandler(this, new EventArgs());
      }
    }
  }
}