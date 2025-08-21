package com.personBlog.adminPanel.Services;

import com.personBlog.adminPanel.Services.Models.PostDetailViewModel;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

import java.util.Optional;
import java.util.UUID;

@Service
public class PostApiService {
    @Value("${blog.url}")
    private String baseUrl;
    @Value("${blog.token}")
    private String token;

    public Optional<PostDetailViewModel> getPostInfoById(UUID id) {

        var restTemplate = new RestTemplate();
        var url = String.format("%s/video/api/Post/post/%s", baseUrl, id);


        var headers = new HttpHeaders();
        headers.set("Authorization", "Bearer " + token);
        headers.set("Content-Type", "application/json");
        headers.set("X-Custom-Header", "custom-value");

        var entity = new HttpEntity<String>(headers);
        try {
            var result = restTemplate.exchange(url, HttpMethod.GET, entity, PostDetailViewModel.class);

            if (result.getStatusCode() == HttpStatus.OK) {
                return Optional.of(result.getBody());
            }
            return Optional.empty();
        } catch (RestClientException e) {
            return  Optional.empty();
        }
    }

}
