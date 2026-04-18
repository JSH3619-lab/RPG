export type RevisionMeta = {
  revision: string;
  displayRevision: string;
};

export type LookupField = {
  key: string;
  label: string;
  section: string;
  visible: boolean;
  allowFreeText: boolean;
  options: string[];
};

export type LookupPage = {
  revision: string;
  displayRevision: string;
  fields: LookupField[];
};

export type GeneratedPartRow = {
  kind: string;
  partCode: string;
  name: string;
  generalInfo: string;
  specification: string;
  note?: string | null;
};

export type IncomingCompRequest = {
  revision: string;
  sourceCode: string;
  dramTypeCode: string;
  densityCode: string;
  bitOrganizationCode: string;
  bankCode: string;
  interfaceCode: string;
  revisionCode: string;
  compTypeCode: string;
  dieBrandCode: string;
  vendorCode: string;
  purchaserCode: string;
  compType2Code: string;
  packageTypeCode: string;
  testerCode: string;
};

export type ModuleRequest = {
  revision: string;
  moduleSourceCode: string;
  compFullPartCode: string;
  moduleFullPartCode: string;
  dramTypeCode: string;
  dimmTypeCode: string;
  moduleDensityCode: string;
  dieDensityCode: string;
  compositionCode: string;
  rankCode: string;
  generationCode: string;
  icBrandCode: string;
  moduleCompTypeCode: string;
  compTestCode: string;
  moduleSmtCode: string;
  moduleTestCode: string;
  speedCode: string;
  pcbCode: string;
  vendorCode: string;
  purchaserCode: string;
  a100SpecialCode: string;
  specialCode2Code: string;
  specialCode3Code: string;
  gradeCode: string;
  productBinCode: string;
  basePartCode: string;
  binPartCode: string;
};
