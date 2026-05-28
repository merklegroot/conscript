export type AspectRatio =
  | "1:1"
  | "3:4"
  | "4:3"
  | "9:16"
  | "16:9"
  | "2:3"
  | "3:2"
  | "auto";

export type ImageResolution = "1k" | "2k";

export type GrokImageGenerationRequest = {
  model: string;
  prompt: string;
  aspect_ratio?: AspectRatio;
  resolution?: ImageResolution;
  response_format: "b64_json";
  n: number;
};

export type GrokImageData = {
  b64_json?: string | null;
  url?: string | null;
  mime_type?: string | null;
};

export type GrokImageGenerationResponse = {
  data: GrokImageData[];
  usage?: {
    cost_in_usd_ticks?: number;
  };
};

export type GenerationRecord = {
  id: string;
  createdAt: string;
  imageFile: string;
  metadataFile: string;
  grok: {
    endpoint: string;
    request: GrokImageGenerationRequest;
    response: GrokImageGenerationResponse;
  };
};

export type GenerateApiSuccess = {
  id: string;
  imageFile: string;
  metadataFile: string;
  imageUrl: string;
  record: GenerationRecord;
};
