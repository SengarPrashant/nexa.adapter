# Implementation Plan: LLM Provider Abstraction

## Overview

This implementation plan breaks down the LLM Provider Abstraction feature into incremental coding tasks. The implementation follows a bottom-up approach: starting with core data models and interfaces, then building provider implementations, and finally wiring everything together with the factory pattern. Each task builds on previous work to ensure continuous integration.

## Tasks

- [x] 1. Set up project structure and core data models
  - Create .NET class library project `LLMProviderAbstraction`
  - Create test project `LLMProviderAbstraction.Tests` with xUnit, Hedgehog, Moq, and FluentAssertions
  - Implement `MessageRole` enum (User, Assistant, System)
  - Implement `Message` class with Content, Role, and Timestamp properties
  - Implement `ErrorType` enum (ValidationError, ConnectionError, AuthenticationError, RateLimitError, ProviderError, UnknownError)
  - Implement `LLMError` class with Type, Message, and InnerException properties
  - Implement `LLMResponse` class with Success, Content, Error, and Metadata properties, including CreateSuccess and CreateError factory methods
  - Implement `ProviderType` enum (Bedrock, Local)
  - _Requirements: 1.1, 1.2, 2.2, 2.3, 5.1, 5.2, 5.3_

- [x] 1.1 Write unit tests for data models
  - Test Message creation with all roles
  - Test LLMResponse factory methods
  - Test LLMError construction with and without inner exceptions
  - Test edge cases (empty strings, null values where allowed)
  - _Requirements: 1.1, 1.2, 2.2, 2.3, 5.1, 5.2, 5.3_

- [ ] 2. Implement configuration and validation
  - [x] 2.1 Create LLMConfiguration class
    - Implement properties: ProviderType, ModelIdentifier, AccessKey, SecretKey, Region, Endpoint, TimeoutSeconds (default 30), MaxRetries (default 3)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 2.2 Create ValidationResult class
    - Implement Success property and error messages collection
    - Implement static Success() and Failure(errors) factory methods
    - _Requirements: 5.1_
  
  - [x] 2.3 Implement configuration validation logic
    - Validate ProviderType is set
    - Validate ModelIdentifier is non-empty
    - For Bedrock: validate AccessKey and SecretKey are provided
    - For Local: validate Endpoint is provided and is valid URI
    - Validate TimeoutSeconds > 0 and MaxRetries >= 0
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1_
  
  - [x] 2.4 Write unit tests for configuration validation
    - Test valid Bedrock configuration
    - Test valid Local configuration
    - Test missing required fields for each provider type
    - Test invalid endpoint URI format
    - Test invalid timeout and retry values
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1_
  
  - [x] 2.5 Write property test for configuration validation
    - **Property 10: Invalid Configuration Returns Validation Error**
    - **Validates: Requirements 5.1**
    - Generate invalid configurations and verify validation fails with descriptive errors
    - _Requirements: 5.1_

- [ ] 3. Implement Session and SessionManager
  - [x] 3.1 Create Session class
    - Implement SessionId, CreatedAt, LastAccessedAt properties
    - Implement private message list with public IReadOnlyList<Message> Messages property
    - Implement AddMessage method that updates LastAccessedAt
    - _Requirements: 3.1, 3.2, 3.4_
  
  - [x] 3.2 Create ISessionManager interface
    - Define CreateSession(string? sessionId = null) method
    - Define GetSession(string sessionId) method
    - Define GetSessionHistory(string sessionId) method
    - _Requirements: 3.1, 3.4_
  
  - [x] 3.3 Implement SessionManager class
    - Use ConcurrentDictionary for thread-safe session storage
    - Implement CreateSession with auto-generated GUID if sessionId not provided
    - Implement GetSession to retrieve existing sessions
    - Implement GetSessionHistory to return read-only message list
    - _Requirements: 3.1, 3.2, 3.4_
  
  - [x] 3.4 Write unit tests for Session and SessionManager
    - Test session creation with and without specified ID
    - Test adding messages to session
    - Test retrieving session history
    - Test concurrent session access
    - Test session not found scenarios
    - _Requirements: 3.1, 3.2, 3.4_
  
  - [x] 3.5 Write property test for session creation
    - **Property 6: Session Creation Always Succeeds**
    - **Validates: Requirements 3.1**
    - Generate random session IDs (including null) and verify session creation succeeds
    - _Requirements: 3.1_
  
  - [x] 3.6 Write property test for message storage
    - **Property 7: Message Storage Round Trip**
    - **Validates: Requirements 3.2, 3.4**
    - Generate random messages, add to session, verify retrieval preserves content and order
    - _Requirements: 3.2, 3.4_

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Create core provider interfaces
  - [x] 5.1 Create ILLMProvider interface
    - Define AnalyzeAsync(string context, string prompt, CancellationToken cancellationToken = default) method
    - Define SendMessageAsync(Session session, string message, CancellationToken cancellationToken = default) method
    - Define ValidateAsync(CancellationToken cancellationToken = default) method
    - _Requirements: 2.1, 2.2, 3.3, 5.1, 6.2_

- [ ] 6. Implement BedrockProvider
  - [x] 6.1 Create BedrockProvider class implementing ILLMProvider
    - Add constructor accepting LLMConfiguration
    - Initialize AmazonBedrockRuntimeClient with credentials and region from config
    - Store model identifier from config
    - _Requirements: 1.1, 1.3, 4.1, 4.2, 4.4, 4.5_
  
  - [x] 6.2 Implement BedrockProvider.ValidateAsync
    - Test connectivity to Bedrock service
    - Map AWS exceptions to appropriate ErrorTypes (AccessDeniedException → AuthenticationError, etc.)
    - Return ValidationResult with descriptive error messages
    - _Requirements: 1.3, 1.5, 5.1_
  
  - [x] 6.3 Implement BedrockProvider.AnalyzeAsync
    - Format context and prompt for Bedrock Converse API
    - Send request using InvokeModelAsync
    - Parse response and extract content
    - Handle errors and map to LLMError types (ThrottlingException → RateLimitError, etc.)
    - Implement retry logic with exponential backoff for transient errors
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 5.2, 5.3, 6.2_
  
  - [x] 6.4 Implement BedrockProvider.SendMessageAsync
    - Convert session message history to Bedrock message format
    - Add new user message to request
    - Send request with full conversation context
    - Parse response and add assistant message to session
    - Handle errors with descriptive messages
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 3.3, 5.2, 5.3, 6.2_
  
  - [x] 6.5 Write unit tests for BedrockProvider
    - Mock IAmazonBedrockRuntime interface
    - Test successful AnalyzeAsync call
    - Test successful SendMessageAsync with session history
    - Test authentication errors (AccessDeniedException)
    - Test rate limit errors (ThrottlingException)
    - Test network errors
    - Test retry logic for transient errors
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 3.3, 5.2, 5.3_
  
  - [x] 6.6 Write property test for provider initialization
    - **Property 2: Provider Initialization with Valid Settings**
    - **Validates: Requirements 1.3, 1.4**
    - Generate valid Bedrock configurations and verify initialization succeeds
    - _Requirements: 1.3, 1.4_
  
  - [x] 6.7 Write property test for authentication failures
    - **Property 3: Authentication and Connection Failures Return Descriptive Errors**
    - **Validates: Requirements 1.5**
    - Simulate authentication failures and verify descriptive error responses
    - _Requirements: 1.5_

- [ ] 7. Implement LocalProvider
  - [x] 7.1 Create LocalProvider class implementing ILLMProvider
    - Add constructor accepting LLMConfiguration and HttpClient
    - Validate endpoint is provided and set timeout from config
    - Store model identifier and endpoint
    - _Requirements: 1.2, 1.4, 4.1, 4.3, 4.4_
  
  - [x] 7.2 Implement LocalProvider.ValidateAsync
    - Test endpoint connectivity with simple HTTP request
    - Handle network errors (connection refused, timeout, DNS failure)
    - Return ValidationResult with descriptive error messages
    - _Requirements: 1.4, 1.5, 5.1, 5.2_
  
  - [x] 7.3 Implement LocalProvider.AnalyzeAsync
    - Format context and prompt for OpenAI-compatible API (/v1/chat/completions)
    - Send POST request with JSON payload
    - Parse JSON response and extract content
    - Handle HTTP errors and map to LLMError types (429 → RateLimitError, 401/403 → AuthenticationError, etc.)
    - Implement retry logic with exponential backoff for transient errors
    - _Requirements: 1.2, 2.1, 2.2, 2.3, 5.2, 5.3, 6.2_
  
  - [x] 7.4 Implement LocalProvider.SendMessageAsync
    - Convert session message history to OpenAI message format
    - Add new user message to request
    - Send request with full conversation context
    - Parse response and add assistant message to session
    - Handle errors with descriptive messages
    - _Requirements: 1.2, 2.1, 2.2, 2.3, 3.3, 5.2, 5.3, 6.2_
  
  - [x] 7.5 Write unit tests for LocalProvider
    - Use HttpMessageHandler mocking or WireMock.Net
    - Test successful AnalyzeAsync call
    - Test successful SendMessageAsync with session history
    - Test various HTTP error codes (400, 401, 404, 429, 500)
    - Test network timeout
    - Test connection refused
    - Test retry logic for transient errors
    - _Requirements: 1.2, 1.4, 1.5, 2.1, 2.2, 2.3, 3.3, 5.2, 5.3_
  
  - [x] 7.6 Write property test for context analysis round trip
    - **Property 4: Context Analysis Request-Response Round Trip**
    - **Validates: Requirements 2.1, 2.2**
    - Generate random context and prompt strings, verify response contains content on success
    - _Requirements: 2.1, 2.2_
  
  - [x] 7.7 Write property test for provider errors
    - **Property 5: Provider Errors Return Descriptive Messages**
    - **Validates: Requirements 2.3**
    - Simulate various provider errors and verify descriptive error messages
    - _Requirements: 2.3_
  
  - [x] 7.8 Write property test for network errors
    - **Property 11: Network Errors Return Connection Error**
    - **Validates: Requirements 5.2**
    - Simulate network failures and verify ConnectionError type with descriptive messages
    - _Requirements: 5.2_
  
  - [x] 7.9 Write property test for rate limit errors
    - **Property 12: Rate Limit Errors Return Rate Limit Error**
    - **Validates: Requirements 5.3**
    - Simulate rate limit responses and verify RateLimitError type with descriptive messages
    - _Requirements: 5.3_

- [x] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Implement factory and wiring
  - [x] 9.1 Create LLMProviderFactory class
    - Add constructor accepting IHttpClientFactory
    - Implement CreateProvider(LLMConfiguration config) method
    - Use switch expression on ProviderType to instantiate correct provider
    - Throw ArgumentException for unsupported provider types
    - _Requirements: 1.1, 1.2, 4.1_
  
  - [x] 9.2 Write unit tests for LLMProviderFactory
    - Test creating BedrockProvider with Bedrock configuration
    - Test creating LocalProvider with Local configuration
    - Test exception thrown for invalid provider type
    - Verify correct provider type is returned
    - _Requirements: 1.1, 1.2, 4.1_
  
  - [x] 9.3 Write property test for provider creation
    - **Property 1: Provider Creation from Valid Configuration**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2, 4.3, 4.4**
    - Generate valid configurations and verify factory creates correct provider type
    - _Requirements: 1.1, 1.2, 4.1, 4.2, 4.3, 4.4_

- [ ] 10. Implement property test for session context inclusion
  - [x] 10.1 Write property test for session context
    - **Property 8: Session Context Inclusion**
    - **Validates: Requirements 3.3**
    - Generate sessions with message history, verify provider includes all messages in request
    - _Requirements: 3.3_

- [ ] 11. Implement property test for region configuration
  - [x] 11.1 Write property test for region propagation
    - **Property 9: Region Configuration Propagation**
    - **Validates: Requirements 4.5**
    - Generate configurations with various regions, verify provider uses specified region
    - _Requirements: 4.5_

- [ ] 12. Implement property test for async operations
  - [x] 12.1 Write property test for async patterns
    - **Property 13: All I/O Operations Are Async**
    - **Validates: Requirements 6.2**
    - Verify all ILLMProvider and ISessionManager methods return Task or Task<T>
    - _Requirements: 6.2_

- [ ] 13. Create test data generators
  - [x] 13.1 Implement ConfigurationGenerators class
    - Create generator for valid Bedrock configurations
    - Create generator for valid Local configurations
    - Create generator for invalid configurations (missing fields, malformed URIs)
    - Create generator for random model identifiers
    - Create generator for random AWS regions
    - _Requirements: 1.1, 1.2, 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 13.2 Implement MessageGenerators class
    - Create generator for random message content (various lengths, character sets)
    - Create generator for all MessageRole values
    - Create generator for edge case messages (empty, very long, special characters)
    - _Requirements: 3.2_
  
  - [x] 13.3 Implement SessionGenerators class
    - Create generator for random session IDs
    - Create generator for sessions with various message history lengths (0-100 messages)
    - Create generator for sessions with mixed message roles
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Run all unit tests and property tests
  - Verify minimum 100 iterations for each property test
  - Ensure all 13 correctness properties are implemented and passing
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples, edge cases, and error conditions
- Checkpoints ensure incremental validation throughout implementation
- All I/O operations use async/await patterns per .NET best practices
- Error handling includes retry logic with exponential backoff for transient errors
